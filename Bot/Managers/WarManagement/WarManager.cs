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

    private readonly Dictionary<ArmySupervisor, Region> _targets = new Dictionary<ArmySupervisor, Region>();

    // TODO GD Use queens?
    private static readonly HashSet<uint> ManageableUnitTypes = Units.ZergMilitary.Except(new HashSet<uint> { Units.Queen, Units.QueenBurrowed }).ToHashSet();

    private bool _rushTagged = false;
    private bool _rushInProgress;
    private HashSet<Unit> _expandsInDanger = new HashSet<Unit>();

    private bool _hasAssaultStarted = false;
    private readonly HashSet<Unit> _soldiers = new HashSet<Unit>();

    private readonly ArmySupervisor _defenseSupervisor = new ArmySupervisor();
    private readonly ArmySupervisor _groundAttackSupervisor = new ArmySupervisor();
    private readonly ArmySupervisor _airAttackSupervisor = new ArmySupervisor();

    private readonly List<BuildRequest> _buildRequests = new List<BuildRequest>();
    private bool _terranFinisherInitiated = false;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _buildRequests.Select(buildRequest => buildRequest.Fulfillment);

    protected override IAssigner Assigner { get; }
    protected override IDispatcher Dispatcher { get; }
    protected override IReleaser Releaser { get; }

    public WarManager() {
        Assigner = new WarManagerAssigner(this);
        Dispatcher = new WarManagerDispatcher(this);
        Releaser = new WarManagerReleaser(this);

        _targets[_defenseSupervisor] = null;
        _targets[_groundAttackSupervisor] = null;
        _targets[_airAttackSupervisor] = null;
    }

    protected override void RecruitmentPhase() {
        HandleRushes();

        Assign(Controller.GetUnits(UnitsTracker.NewOwnedUnits, ManageableUnitTypes));
    }

    // TODO GD Figure out an attack / retreat pattern, right now if the army retreats it doesn't go back to attack
    protected override void DispatchPhase() {
        var attackTarget = _targets[_groundAttackSupervisor];
        if (!_hasAssaultStarted) {
            // Start the attack
            if (attackTarget != null && _soldiers.GetForce() > RegionTracker.GetForce(attackTarget, Alliance.Enemy)) {
                _hasAssaultStarted = true;

                // TODO GD Should be called ReleaseAll?
                _defenseSupervisor.Retire();
                // TODO GD Dispatch only has the logic to dispatch to the attack supervisors
                Dispatch(_soldiers.Where(soldier => soldier.Supervisor == null));
            }
            // Keep defending
            else {
                foreach (var soldier in _soldiers.Where(soldier => soldier.Supervisor == null)) {
                    _defenseSupervisor.Assign(soldier);
                }
            }
        }
        // Abort the attack
        else if (attackTarget != null && _groundAttackSupervisor.Army.GetForce() < RegionTracker.GetForce(_targets[_groundAttackSupervisor], Alliance.Enemy)) {
            _groundAttackSupervisor.Retire();
            foreach (var soldier in _soldiers.Where(soldier => soldier.Supervisor == null)) {
                _defenseSupervisor.Assign(soldier);
            }
        }
        // Keep attacking
        else {
            _defenseSupervisor.Retire();
            Dispatch(_soldiers.Where(soldier => soldier.Supervisor == null));
        }
    }

    // TODO GD Add graphical debugging to show regions to attack/defend
    protected override void ManagementPhase() {
        // Determine regions to defend
        var regionToDefend = RegionAnalyzer.Regions.MaxBy(region => RegionTracker.GetDefenseScore(region))!;
        _defenseSupervisor.AssignTarget(regionToDefend.Center, ApproximateRegionRadius(regionToDefend));
        _targets[_defenseSupervisor] = regionToDefend;

        // Determine regions to attack
        // TODO GD This makes the attack target switch as we destroy buildings. At some point, fog of war becomes more interesting and we won't hunt the remaining buildings
        var valuableEnemyRegions = RegionAnalyzer.Regions
            .Where(region => RegionTracker.GetValue(region, Alliance.Enemy) > UnitEvaluator.Value.Intriguing)
            .ToList();

        if (!valuableEnemyRegions.Any()) {
            valuableEnemyRegions = RegionAnalyzer.Regions;
        }

        var regionToAttack = valuableEnemyRegions
            .MaxBy(region => RegionTracker.GetValue(region, Alliance.Enemy) / RegionTracker.GetForce(region, Alliance.Enemy))!;

        _groundAttackSupervisor.AssignTarget(regionToAttack.Center, ApproximateRegionRadius(regionToAttack), canHuntTheEnemy: true);
        _targets[_groundAttackSupervisor] = regionToAttack;

        _groundAttackSupervisor.AssignTarget(regionToAttack.Center, 999, canHuntTheEnemy: true);
        _targets[_airAttackSupervisor] = regionToAttack;

        // TODO GD Request more forces (i.e zerglings when rushed? or is it the build manager?)
        // TODO GD Handle this better
        if (_hasAssaultStarted && _buildRequests.Count == 0) {
            _buildRequests.Add(new TargetBuildRequest(BuildType.Train, Units.Roach, targetQuantity: 100));
        }

        if (_hasAssaultStarted && ShouldFinishOffTerran()) {
            FinishOffTerran();
        }

        // TODO GD Send this task to the supervisor instead
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
}
