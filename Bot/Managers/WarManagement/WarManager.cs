using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Builds;
using Bot.Debugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
using Bot.Managers.WarManagement.ArmySupervision;
using Bot.MapKnowledge;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement;

/*
 * REFACTOR NOTES
 * Use a strategy to do each phase
 * - i.e EarlyGameRecruitmentPhaseStrategy
 *
 * Use a state machine to set the strategies, dispatchers, assigners and releasers
 *
 * Strategies themselves can use strategies or other states
 * - TerranFinisher
 * - CannonRush defense
 * - WorkerRush defense
 *
 * Be conservative about switching states try not to yo-yo
 */

public partial class WarManager: Manager {
    private readonly WarManagerBehaviour _behaviour;

    [Flags]
    public enum Stance {
        None = 0,
        Attack = 1,
        Defend = 2,
        Finisher = 4,
        TerranFinisher = 8,
    }

    private const int RushTimingInSeconds = (int)(5 * 60);
    private const float RequiredForceRatioBeforeAttacking = 1.5f;
    private const float MaxSupplyBeforeAttacking = 175;

    private static readonly HashSet<uint> ManageableUnitTypes = Units.ZergMilitary.Except(new HashSet<uint> { Units.Queen, Units.QueenBurrowed }).ToHashSet();

    private bool _rushTagged = false;
    private bool _isRushInProgress = false;

    private Stance _stance = Stance.Defend;
    private readonly HashSet<Unit> _soldiers = new HashSet<Unit>();

    private readonly ArmySupervisor _defenseSupervisor = new ArmySupervisor();
    private readonly ArmySupervisor _attackSupervisor = new ArmySupervisor();
    private readonly ArmySupervisor _terranFinisherSupervisor = new ArmySupervisor();

    private static BuildRequest _corruptorsBuildRequest = new TargetBuildRequest(BuildType.Train, Units.Corruptor, targetQuantity: 10, priority: BuildRequestPriority.VeryHigh, blockCondition: BuildBlockCondition.All);
    private static BuildRequest _armyBuildRequest = new TargetBuildRequest(BuildType.Train, Units.Roach, targetQuantity: 100, priority: BuildRequestPriority.Low);
    private readonly List<BuildRequest> _buildRequests = new List<BuildRequest>();
    public override IEnumerable<BuildFulfillment> BuildFulfillments => _buildRequests.Select(buildRequest => buildRequest.Fulfillment);

    private readonly WarManagerDebugger _debugger = new WarManagerDebugger();

    protected override IAssigner Assigner => _behaviour.Assigner;
    protected override IDispatcher Dispatcher => _behaviour.Dispatcher;
    protected override IReleaser Releaser => _behaviour.Releaser;

    public WarManager() {
        _behaviour = new WarManagerBehaviour(this);
        _buildRequests.Add(_armyBuildRequest);

        // TODO GD Probably do the same thing with attack supervisor at some point to speed up the end of the game
        _terranFinisherSupervisor.AssignTarget(new Vector2(MapAnalyzer.MaxX / 2f, MapAnalyzer.MaxY / 2f), 999, canHuntTheEnemy: true);
    }

    public override string ToString() {
        return "WarManager";
    }

    protected override void StartOfFramePhase() {
        _behaviour.Update();
    }

    protected override void RecruitmentPhase() {
        HandleRushes();

        Assign(Controller.GetUnits(UnitsTracker.NewOwnedUnits, ManageableUnitTypes));
    }

    protected override void DispatchPhase() {
        Dispatch(_soldiers.Where(soldier => soldier.Supervisor == null));
    }

    protected override void ManagementPhase() {
        if (TheGameIsOurs()) {
            FinishHim();
        }
        else {
            BigBrainPlay();
        }

        AdjustBuildRequests();

        if (ShouldFinishOffTerran()) {
            FinishOffTerran();
        }

        // TODO GD Send this task to the supervisor instead?
        if (ShouldFreeSomeSupply()) {
            FreeSomeSupply();
        }
        else {
            _attackSupervisor.OnFrame();
            _defenseSupervisor.OnFrame();
        }

        _terranFinisherSupervisor.OnFrame();

        _debugger.Debug(ManagedUnits);
    }

    private void HandleRushes() {
        if (!_isRushInProgress && Controller.Frame > TimeUtils.SecsToFrames(RushTimingInSeconds)) {
            // We do not want to trigger a rush defense past a certain point
            return;
        }

        // TODO GD Hold these in a separate lists
        var draftedUnits = ManagedUnits
            .Where(soldier => !ManageableUnitTypes.Contains(soldier.UnitType))
            .ToList();

        // TODO GD To do this we need the eco manager to not send them to a dangerous expand
        //Release(draftedUnits.Where(unit => unit.HitPoints <= 10));

        _isRushInProgress = DangerScanner.IsRushInProgress(ManagedUnits.Except(draftedUnits).ToList());

        if (_isRushInProgress) {
            if (!_rushTagged) {
                TaggingService.TagGame(TaggingService.Tag.EarlyAttack);
                _rushTagged = true;
            }

            var townHallSupervisors = Controller
                .GetUnits(UnitsTracker.OwnedUnits, Units.TownHalls)
                .Where(unit => unit.Supervisor != null)
                .Select(supervisedTownHall => supervisedTownHall.Supervisor)
                .ToList();

            var queensToDraft = new List<Unit>();
            var draftableDrones = new List<Unit>();
            foreach (var townHallSupervisor in townHallSupervisors) {
                queensToDraft.AddRange(Controller.GetUnits(townHallSupervisor.SupervisedUnits, Units.Queen));
                draftableDrones.AddRange(Controller.GetUnits(townHallSupervisor.SupervisedUnits, Units.Drone).Skip(2));
            }

            Assign(queensToDraft);

            draftableDrones = draftableDrones
                .OrderByDescending(drone => drone.Integrity)
                // TODO GD This could be better, it assumes the threat comes from the natural
                .ThenBy(drone => drone.DistanceTo(ExpandAnalyzer.GetExpand(Alliance.Self, ExpandType.Natural).Position))
                .ToList();

            var enemyForce = GetEnemyForce();
            var draftIndex = 0;
            while (draftIndex < draftableDrones.Count && _soldiers.GetForce() < enemyForce) {
                Assign(draftableDrones[draftIndex]);
                draftIndex++;
            }
        }
        else {
            Release(draftedUnits);
        }
    }

    // TODO GD Probably need a class for this
    /// <summary>
    /// Some Terran will fly their buildings.
    /// Check if they are basically dead and we should start dealing with the flying buildings.
    /// </summary>
    /// <returns>True if we should start handling flying terran buildings</returns>
    private bool ShouldFinishOffTerran() {
        if (Controller.EnemyRace != Race.Terran) {
            return false;
        }

        if (!_stance.HasFlag(Stance.Attack)) {
            return false;
        }

        if (Controller.Frame < TimeUtils.SecsToFrames(10 * 60)) {
            return false;
        }

        if (Controller.Frame % TimeUtils.SecsToFrames(5) != 0) {
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
        if (_stance.HasFlag(Stance.TerranFinisher)) {
            return;
        }

        _buildRequests.Add(new TargetBuildRequest(BuildType.Build, Units.Spire, targetQuantity: 1, priority: BuildRequestPriority.VeryHigh, blockCondition: BuildBlockCondition.All));
        _buildRequests.Add(_corruptorsBuildRequest);

        _stance |= Stance.TerranFinisher;
        TaggingService.TagGame(TaggingService.Tag.TerranFinisher);
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
        foreach (var supervisedUnit in _attackSupervisor.SupervisedUnits.Where(unit => unit.IsBurrowed)) {
            supervisedUnit.UseAbility(Abilities.BurrowRoachUp);
        }

        var unburrowedUnits = _attackSupervisor.SupervisedUnits.Where(unit => !unit.IsBurrowed).ToList();
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
        // TODO GD Maybe consider forces in regions adjacent to the path?
        // TODO GD Also consider the distance to the region to simulate commitment? Or only change target if significant value change
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
        if (regionToAttack.IsObstructed) {
            Logger.Error("Trying to attack an obstructed region");
            return;
        }

        if (ManagedUnits.Count == 0) {
            // TODO GD Should we do stuff here?
            return;
        }

        var armyRegion = ManagedUnits.GetRegion();
        if (armyRegion == null) {
            Logger.Error("AttackOrDefend could not find the region of its army of size {0}", ManagedUnits.Count);
            return;
        }

        var pathToTarget = Pathfinder.FindPath(armyRegion, regionToAttack);
        if (pathToTarget == null) {
            Logger.Error("AttackOrDefend could not find a path from the army to its target {0} -> {1}", armyRegion, regionToAttack);
            return;
        }

        var ourForce = ManagedUnits.GetForce();

        // TODO GD Maybe consider units near the path as well?
        var enemyForce = pathToTarget.Sum(region => RegionTracker.GetForce(region, Alliance.Enemy));

        if (ourForce > enemyForce * RequiredForceRatioBeforeAttacking || Controller.CurrentSupply >= MaxSupplyBeforeAttacking) {
            if (_stance.HasFlag(Stance.Defend)) {
                _defenseSupervisor.Retire();
            }

            _stance |= Stance.Attack;
            _stance &= ~Stance.Defend; // Unset the defend flag
            _debugger.Target = regionToAttack;
        }
        else {
            if (_stance.HasFlag(Stance.Attack)) {
                _attackSupervisor.Retire();
            }

            _stance = Stance.Defend;
            _debugger.Target = regionToDefend;
        }

        _debugger.CurrentStance = _stance;
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
        var enemyForce = GetEnemyForce();

        // This is a small tweak to prevent blocking the build when getting scouted (LOL)
        // TODO GD We need a way to tell that an enemy drone is hostile to better handle this
        if (enemyForce <= 2) {
            enemyForce = 0;
        }

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
        if (Controller.IsUnlocked(Units.Roach, TechTree.UnitPrerequisites)) {
            return Units.Roach;
        }

        if (Controller.GetUnits(UnitsTracker.OwnedUnits, Units.SpawningPool).Any()) {
            return Units.Zergling;
        }

        // TODO GD Not sure if this is good
        return Units.Drone;
    }

    /// <summary>
    /// Evaluates if we are overwhelming the opponent.
    /// </summary>
    /// <returns>True if we can stop being fancy and just finish the opponent</returns>
    private bool TheGameIsOurs() {
        if (!_stance.HasFlag(Stance.Finisher) && MapAnalyzer.VisibilityRatio < 0.85) {
            return false;
        }

        var ourForce = _soldiers.GetForce();
        var enemyForce = GetEnemyForce();
        if (ourForce < enemyForce * 3) {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Steamroll the opponent.
    /// </summary>
    private void FinishHim() {
        if (_stance.HasFlag(Stance.Finisher)) {
            return;
        }

        var target = _soldiers.GetCenter();
        _attackSupervisor.AssignTarget(target, 999, canHuntTheEnemy: true);

        if (_stance.HasFlag(Stance.Defend)) {
            _defenseSupervisor.Retire();
        }

        _stance |= Stance.Attack | Stance.Finisher;
        _stance &= ~Stance.Defend; // Unset the defend flag

        _debugger.CurrentStance = _stance;
        _debugger.Target = target.GetRegion();
    }

    /// <summary>
    /// Decide between attack and defense and pick good targets based on current map control.
    /// </summary>
    private void BigBrainPlay() {
        // Determine regions to defend
        var regionToDefend = GetRegionToDefend();
        _defenseSupervisor.AssignTarget(regionToDefend.Center, ApproximateRegionRadius(regionToDefend), canHuntTheEnemy: false);

        // Determine regions to attack
        var regionToAttack = GetRegionToAttack();
        _attackSupervisor.AssignTarget(regionToAttack.Center, ApproximateRegionRadius(regionToAttack), canHuntTheEnemy: true);

        AttackOrDefend(regionToAttack, regionToDefend);
    }

    /// <summary>
    /// Determines if supply should be freed to let the TerranFinisher do its job
    /// </summary>
    /// <returns></returns>
    private bool ShouldFreeSomeSupply() {
        if (!_stance.HasFlag(Stance.TerranFinisher)) {
            return false;
        }

        if (Controller.AvailableSupply >= 2) {
            return false;
        }

        return _corruptorsBuildRequest.Fulfillment.Remaining > 0;
    }

    /// <summary>
    /// Returns the enemy force
    /// </summary>
    /// <returns></returns>
    private static float GetEnemyForce() {
        return UnitsTracker.EnemyMemorizedUnits.Values.Concat(UnitsTracker.EnemyUnits).GetForce();
    }
}
