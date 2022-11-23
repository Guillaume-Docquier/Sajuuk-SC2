using System;
using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
using Bot.Managers.ArmySupervision;
using Bot.MapKnowledge;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement;

public partial class WarManager: Manager {
    private const int RushTimingInSeconds = (int)(5 * 60);

    private static readonly HashSet<uint> ManageableUnitTypes = Units.ZergMilitary.Except(new HashSet<uint> { Units.Queen, Units.QueenBurrowed }).ToHashSet();

    private bool _rushTagged = false;
    private bool _rushInProgress;
    private HashSet<Unit> _expandsInDanger = new HashSet<Unit>();

    private bool _isAttacking = false;
    private readonly HashSet<Unit> _soldiers = new HashSet<Unit>();

    private readonly ArmySupervisor _defenseSupervisor = new ArmySupervisor();
    private readonly ArmySupervisor _groundAttackSupervisor = new ArmySupervisor();
    private readonly ArmySupervisor _airAttackSupervisor = new ArmySupervisor();

    private readonly List<BuildRequest> _buildRequests = new List<BuildRequest>();
    private bool _terranFinisherInitiated = false;

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

        // Defend by default
        // Try to see if the army could attack, if yes, do
        // If the army is too weak, call it off
        // TODO GD Only attack when we beat UnitsTracker.EnemyMemorizedUnits?
        if (_soldiers.GetForce() > RegionTracker.GetForce(regionToAttack, Alliance.Enemy)) {
            _isAttacking = true;
            _defenseSupervisor.Retire();

            _groundAttackSupervisor.AssignTarget(regionToAttack.Center, ApproximateRegionRadius(regionToAttack), canHuntTheEnemy: true);
            _airAttackSupervisor.AssignTarget(regionToAttack.Center, 999, canHuntTheEnemy: true);
        }
        else {
            _isAttacking = false;
            _groundAttackSupervisor.Retire();
            _airAttackSupervisor.Retire();
        }

        // TODO GD Improve on this idea, this is great. We can make the order blocker if the pressure is too high
        if (_soldiers.GetForce() < UnitsTracker.EnemyMemorizedUnits.Values.GetForce()) {
            _armyBuildRequest.Priority = BuildRequestPriority.Medium;
        }
        else {
            _armyBuildRequest.Priority = BuildRequestPriority.Low;
        }

        if (_isAttacking && ShouldFinishOffTerran()) {
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

    public override string ToString() {
        return "WarManager";
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

        var regionToAttack = valuableEnemyRegions
            .MaxBy(region => RegionTracker.GetValue(region, Alliance.Enemy) / RegionTracker.GetForce(region, Alliance.Enemy))!;

        return regionToAttack;
    }
}
