using System;
using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
using Bot.Managers.WarManagement.ArmySupervision;
using Bot.MapKnowledge;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement;

public partial class WarManager: Manager {
    public enum Stance {
        Attack,
        Defend,
    }

    private const int RushTimingInSeconds = (int)(5 * 60);

    private static readonly HashSet<uint> ManageableUnitTypes = Units.ZergMilitary.Except(new HashSet<uint> { Units.Queen, Units.QueenBurrowed }).ToHashSet();

    private bool _rushTagged = false;
    private bool _rushInProgress;
    private HashSet<Unit> _expandsInDanger = new HashSet<Unit>();

    private Stance _stance = Stance.Defend;
    private readonly HashSet<Unit> _soldiers = new HashSet<Unit>();

    private readonly ArmySupervisor _defenseSupervisor = new ArmySupervisor();
    private readonly ArmySupervisor _groundAttackSupervisor = new ArmySupervisor();
    private readonly ArmySupervisor _airAttackSupervisor = new ArmySupervisor();

    private readonly List<BuildRequest> _buildRequests = new List<BuildRequest>();
    private bool _terranFinisherInitiated = false;

    private readonly WarManagerDebugger _debugger = new WarManagerDebugger();

    private static BuildRequest _armyBuildRequest = new TargetBuildRequest(BuildType.Train, Units.Roach, targetQuantity: 100, priority: BuildRequestPriority.Low);
    public override IEnumerable<BuildFulfillment> BuildFulfillments => _buildRequests.Select(buildRequest => buildRequest.Fulfillment);

    protected override IAssigner Assigner { get; }
    protected override IDispatcher Dispatcher { get; }
    protected override IReleaser Releaser { get; }

    public WarManager() {
        Assigner = new WarManagerAssigner(this);
        Dispatcher = new WarManagerDispatcher(this);
        Releaser = new WarManagerReleaser(this);

        _buildRequests.Add(_armyBuildRequest);
    }

    public override string ToString() {
        return "WarManager";
    }

    protected override void RecruitmentPhase() {
        HandleRushes();

        Assign(Controller.GetUnits(UnitsTracker.NewOwnedUnits, ManageableUnitTypes));
    }

    protected override void DispatchPhase() {
        Dispatch(_soldiers.Where(soldier => soldier.Supervisor == null));
    }

    // TODO GD Add graphical debugging to show regions to attack/defend
    protected override void ManagementPhase() {
        // Determine regions to defend
        var regionToDefend = GetRegionToDefend();
        _defenseSupervisor.AssignTarget(regionToDefend.Center, ApproximateRegionRadius(regionToDefend), canHuntTheEnemy: false);

        // Determine regions to attack
        var regionToAttack = GetRegionToAttack();
        _groundAttackSupervisor.AssignTarget(regionToAttack.Center, ApproximateRegionRadius(regionToAttack), canHuntTheEnemy: true);
        _airAttackSupervisor.AssignTarget(regionToAttack.Center, 999, canHuntTheEnemy: true);

        AttackOrDefend(regionToAttack, regionToDefend);
        AdjustBuildRequests();

        if (_stance == Stance.Attack && ShouldFinishOffTerran()) {
            FinishOffTerran();
        }

        // TODO GD Send this task to the supervisor instead?
        if (_terranFinisherInitiated && Controller.AvailableSupply < 2) {
            FreeSomeSupply();
        }
        else {
            _groundAttackSupervisor.OnFrame();
            _defenseSupervisor.OnFrame();
        }

        _airAttackSupervisor.OnFrame();

        _debugger.Debug();
    }

    private void ScanForEndangeredExpands() {
        var expandsInDanger = DangerScanner.GetEndangeredExpands().ToHashSet();
        foreach (var expandNewlyInDanger in expandsInDanger.Except(_expandsInDanger)) {
            Logger.Info("({0}) An expand is newly in danger: {1}", this, expandNewlyInDanger);
        }

        foreach (var expandNoLongerInDanger in _expandsInDanger.Except(expandsInDanger)) {
            Logger.Info("({0}) An expand is no longer in danger: {1}", this, expandNoLongerInDanger);
        }

        _expandsInDanger = expandsInDanger;
    }

    private void HandleRushes() {
        ScanForEndangeredExpands();

        if (_expandsInDanger.Count > 0 && Controller.Frame <= TimeUtils.SecsToFrames(RushTimingInSeconds)) {
            if (!_rushTagged) {
                TaggingService.TagGame(TaggingService.Tag.EarlyAttack);
                _rushTagged = true;
            }

            _rushInProgress = true;

            // TODO GD We should be smarter about how many units we draft
            var supervisedTownHalls = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.TownHalls).Where(unit => unit.Supervisor != null);
            foreach (var supervisedTownHall in supervisedTownHalls) {
                var draftedWorkers = supervisedTownHall.Supervisor.SupervisedUnits.Where(unit => Units.Workers.Contains(unit.UnitType)).Skip(2);
                Assign(draftedWorkers);

                var draftedQueens = supervisedTownHall.Supervisor.SupervisedUnits.Where(unit => unit.UnitType == Units.Queen);
                Assign(draftedQueens);
            }
        }
        else if (_rushInProgress && _expandsInDanger.Count <= 0) {
            var unitsToReturn = _soldiers.Where(soldier => Units.Workers.Contains(soldier.UnitType) || soldier.UnitType == Units.Queen);
            Release(unitsToReturn);

            _rushInProgress = false;
        }
    }

    // TODO GD Probably need a class for this
    /// <summary>
    /// Some Terran will fly their buildings.
    /// Check if they are basically dead and we should start dealing with the flying buildings.
    /// </summary>
    /// <returns>True if we should start handling flying terran buildings</returns>
    private static bool ShouldFinishOffTerran() {
        if (Controller.EnemyRace != Race.Terran) {
            return false;
        }

        if (Controller.Frame < TimeUtils.SecsToFrames(12 * 60)) {
            return false;
        }

        if (Controller.Frame % TimeUtils.SecsToFrames(60) != 0) {
            return false;
        }

        if (MapAnalyzer.ExplorationRatio < 0.80 || !ExpandAnalyzer.ExpandLocations.All(expandLocation => VisibilityTracker.IsExplored(expandLocation.Position))) {
            return false;
        }

        return Controller.GetUnits(UnitsTracker.EnemyUnits, Units.Buildings).All(building => building.IsFlying);
    }

    /// <summary>
    /// Create anti-air units to deal with terran flying buildings.
    /// </summary>
    private void FinishOffTerran() {
        if (_terranFinisherInitiated) {
            return;
        }

        _buildRequests.Clear();
        _buildRequests.Add(new TargetBuildRequest(BuildType.Build, Units.Spire, targetQuantity: 1));
        _buildRequests.Add(new TargetBuildRequest(BuildType.Train, Units.Corruptor, targetQuantity: 10));
        _terranFinisherInitiated = true;
    }

    /// <summary>
    /// Approximates the region's radius based on it's cells
    /// This is a placeholder while we have a smarter region defense behaviour
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    private static float ApproximateRegionRadius(Region region) {
        return (float)Math.Sqrt(region.Cells.Count) / 2;
    }

    /// <summary>
    /// Orders the army to kill 1 of their own to free some supply
    /// </summary>
    private void FreeSomeSupply() {
        foreach (var supervisedUnit in _groundAttackSupervisor.SupervisedUnits.Where(unit => unit.IsBurrowed)) {
            supervisedUnit.UseAbility(Abilities.BurrowRoachUp);
        }

        var unburrowedUnits = _groundAttackSupervisor.SupervisedUnits.Where(unit => !unit.IsBurrowed).ToList();
        if (unburrowedUnits.Count > 0) {
            var unitToSacrifice = unburrowedUnits[0];
            foreach (var unburrowedUnit in unburrowedUnits) {
                unburrowedUnit.Attack(unitToSacrifice);
            }
        }
    }

    /// <summary>
    /// Determines which region to defend next
    /// </summary>
    /// <returns>The region to defend next</returns>
    private static Region GetRegionToDefend() {
        // TODO GD We sometimes try to defend a position that's behind the enemy army while we are losing
        return RegionAnalyzer.Regions.MaxBy(region => RegionTracker.GetDefenseScore(region))!;
    }

    /// <summary>
    /// Determines which region to attack next
    /// </summary>
    /// <returns>The region to attack next</returns>
    private static Region GetRegionToAttack() {
        var valuableEnemyRegions = RegionAnalyzer.Regions
            .Where(region => RegionTracker.GetValue(region, Alliance.Enemy) > UnitEvaluator.Value.Intriguing)
            .ToList();

        if (!valuableEnemyRegions.Any()) {
            valuableEnemyRegions = RegionAnalyzer.Regions;
        }

        // TODO GD Checking only the region's force doesn't take into account that maybe we are forced to go through more enemies to get to our target.
        var regionToAttack = valuableEnemyRegions
            .MaxBy(region => RegionTracker.GetValue(region, Alliance.Enemy) / RegionTracker.GetForce(region, Alliance.Enemy))!;

        return regionToAttack;
    }

    /// <summary>
    /// Determines if we should attack the given region or defend our home.
    /// </summary>
    /// <param name="regionToAttack">The region that we would attack</param>
    /// <param name="regionToDefend">The region that we would defend</param>
    private void AttackOrDefend(Region regionToAttack, Region regionToDefend) {
        // Because of the map resolution, some units can actually walk on unwalkable tiles
        // This causes GetRegion() to return null
        // It's fixable, but I need to measure the impact of such a fix first
        var soldiersInARegion = _soldiers.Where(soldier => soldier.GetRegion() != null).ToList();

        if (soldiersInARegion.Count == 0) {
            // TODO GD Should we do stuff here?
            return;
        }

        var ourForce = _soldiers.GetForce();
        var enemyForce = Pathfinder
            .FindPath(soldiersInARegion.GetCenter().GetRegion(), regionToAttack)
            .Sum(region => RegionTracker.GetForce(region, Alliance.Enemy));

        // TODO GD Only attack when we beat UnitsTracker.EnemyMemorizedUnits?
        if (ourForce > enemyForce) {
            if (_stance == Stance.Defend) {
                _defenseSupervisor.Retire();
            }

            _stance = Stance.Attack;
        }
        else {
            if (_stance == Stance.Attack) {
                _groundAttackSupervisor.Retire();
                _airAttackSupervisor.Retire();
            }

            _stance = Stance.Defend;
        }

        _debugger.CurrentStance = _stance;
        _debugger.Target = _stance == Stance.Attack ? regionToAttack : regionToDefend;
    }

    /// <summary>
    /// Request more or less army based on what the WarManager has and what the enemy has and is doing.
    /// </summary>
    private void AdjustBuildRequests() {
        var unitTypeToProduce = GetUnitTypeToProduce();
        if (_armyBuildRequest.UnitOrUpgradeType != unitTypeToProduce) {
            _buildRequests.Remove(_armyBuildRequest);

            _armyBuildRequest = new TargetBuildRequest(BuildType.Train, unitTypeToProduce, targetQuantity: 100, priority: BuildRequestPriority.Low);
            _buildRequests.Add(_armyBuildRequest);
        }

        // TODO GD Consider units in production too
        var ourForce = _soldiers.GetForce();
        // TODO GD Exclude buildings?
        var enemyForce = UnitsTracker.EnemyMemorizedUnits.Values.Concat(UnitsTracker.EnemyUnits).GetForce();

        if (ourForce * 1.5 < enemyForce) {
            _armyBuildRequest.BlockCondition = BuildBlockCondition.MissingMinerals | BuildBlockCondition.MissingProducer | BuildBlockCondition.MissingTech;
        }
        else {
            _armyBuildRequest.BlockCondition = BuildBlockCondition.None;
        }

        // TODO GD Try to see if they are attacking or not. If they're not, keep droning
        if (ourForce < enemyForce) {
            _armyBuildRequest.Priority = BuildRequestPriority.Medium;
        }
        else {
            _armyBuildRequest.Priority = BuildRequestPriority.Low;
        }

        _debugger.OwnForce = ourForce;
        _debugger.EnemyForce = enemyForce;
        _debugger.BuildPriority = _armyBuildRequest.Priority;
        _debugger.BuildBlockCondition = _armyBuildRequest.BlockCondition;
    }

    /// <summary>
    /// Determines the unit type to produce given the current situation.
    /// Right now, it just checks if it can make roaches, but in the future it might check against the enemy composition.
    /// </summary>
    /// <returns>The unit type id to produce.</returns>
    private static uint GetUnitTypeToProduce() {
        if (Controller.IsUnitUnlocked(Units.Roach)) {
            return Units.Roach;
        }

        return Units.Zergling;
    }
}
