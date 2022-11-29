using System;
using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
using Bot.Managers.WarManagement.ArmySupervision;
using Bot.Managers.WarManagement.States.MidGame;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement.States.EarlyGame;

public class EarlyGameBehaviour : IWarManagerBehaviour {
    private static readonly HashSet<uint> ManageableUnitTypes = Units.ZergMilitary.Except(new HashSet<uint> { Units.Queen, Units.QueenBurrowed }).ToHashSet();

    private readonly EarlyGameBehaviourDebugger _debugger = new EarlyGameBehaviourDebugger();
    private readonly WarManager _warManager;
    private readonly HashSet<Region> _startingRegions;
    private BuildRequest _armyBuildRequest = new TargetBuildRequest(BuildType.Train, Units.Roach, targetQuantity: 100, priority: BuildRequestPriority.Low);

    private bool _rushTagged = false;
    private bool _isRushInProgress = false;

    public readonly ArmySupervisor DefenseSupervisor = new ArmySupervisor();

    public IAssigner Assigner { get; }
    public IDispatcher Dispatcher { get; }
    public IReleaser Releaser { get; }

    public List<BuildRequest> BuildRequests { get; } = new List<BuildRequest>();

    public EarlyGameBehaviour(WarManager warManager) {
        _warManager = warManager;
        BuildRequests.Add(_armyBuildRequest);

        Assigner = new WarManagerAssigner<EarlyGameBehaviour>(this);
        Dispatcher = new EarlyGameDispatcher(this);
        Releaser = new WarManagerReleaser<EarlyGameBehaviour>(this);

        var main = ExpandAnalyzer.GetExpand(Alliance.Self, ExpandType.Main).Position.GetRegion();
        var natural = ExpandAnalyzer.GetExpand(Alliance.Self, ExpandType.Natural).Position.GetRegion();
        _startingRegions = Pathfinder.FindPath(main, natural).ToHashSet();
    }

    public void RecruitmentPhase() {
        _warManager.Assign(Controller.GetUnits(UnitsTracker.NewOwnedUnits, ManageableUnitTypes));
        RecruitEcoUnitsIfNecessary();
    }

    public void DispatchPhase() {
        _warManager.Dispatch(_warManager.ManagedUnits.Where(soldier => soldier.Supervisor == null));
    }

    public void ManagementPhase() {
        var regionToDefend = _startingRegions.MaxBy(region => RegionTracker.GetForce(region, Alliance.Enemy))!;
        DefenseSupervisor.AssignTarget(regionToDefend.Center, regionToDefend.ApproximatedRadius, canHuntTheEnemy: false);
        DefenseSupervisor.OnFrame();

        AdjustBuildRequests();

        _debugger.OwnForce = _warManager.ManagedUnits.GetForce();
        _debugger.EnemyForce = GetEnemyForce();
        _debugger.Target = regionToDefend;
        _debugger.CurrentStance = Stance.Defend;
        _debugger.BuildPriority = BuildRequests.FirstOrDefault()?.Priority ?? BuildRequestPriority.Low;
        _debugger.BuildBlockCondition = BuildRequests.FirstOrDefault()?.BlockCondition ?? BuildBlockCondition.None;
        _debugger.Debug();
    }

    // TODO GD Make this part of something
    public bool CleanUp() {
        if (_isRushInProgress) {
            return false;
        }

        var draftedUnits = GetDraftedUnits();
        if (draftedUnits.Any()) {
            _warManager.Release(draftedUnits);

            // We give one tick so that release orders, like stop or unburrow go through
            return false;
        }

        return true;
    }

    /// <summary>
    /// Recruit eco units to help fend off early attacks.
    /// </summary>
    private void RecruitEcoUnitsIfNecessary() {
        var draftedUnits = GetDraftedUnits();

        // TODO GD To do this we need the eco manager to not send them to a dangerous expand
        //Release(draftedUnits.Where(unit => unit.HitPoints <= 10));

        _isRushInProgress = IsRushInProgress(_warManager.ManagedUnits.Except(draftedUnits).ToList());
        if (!_isRushInProgress) {
            _warManager.Release(draftedUnits);
            return;
        }

        if (!_rushTagged) {
            TaggingService.TagGame(TaggingService.Tag.EarlyAttack);
            _rushTagged = true;
        }

        var townHallSupervisors = Controller
            .GetUnits(UnitsTracker.OwnedUnits, Units.TownHalls)
            .Where(unit => unit.Supervisor != null)
            .Select(supervisedTownHall => supervisedTownHall.Supervisor)
            .ToList();

        var draftableDrones = new List<Unit>();
        foreach (var townHallSupervisor in townHallSupervisors) {
            _warManager.Assign(Controller.GetUnits(townHallSupervisor.SupervisedUnits, Units.Queen));

            draftableDrones
                .AddRange(Controller.GetUnits(townHallSupervisor.SupervisedUnits, Units.Drone)
                // TODO GD Maybe sometimes we should take all of them
                .Skip(2));
        }

        draftableDrones = draftableDrones
            .OrderByDescending(drone => drone.Integrity)
            // TODO GD This could be better, it assumes the threat comes from the natural
            .ThenBy(drone => drone.DistanceTo(ExpandAnalyzer.GetExpand(Alliance.Self, ExpandType.Natural).Position))
            .ToList();

        var enemyForce = GetEnemyForce();
        var draftIndex = 0;
        while (draftIndex < draftableDrones.Count && _warManager.ManagedUnits.GetForce() < enemyForce) {
            _warManager.Assign(draftableDrones[draftIndex]);
            draftIndex++;
        }
    }

    /// <summary>
    /// Request more or less army based on what the WarManager has and what the enemy has and is doing.
    /// </summary>
    private void AdjustBuildRequests() {
        var unitTypeToProduce = GetUnitTypeToProduce();
        if (_armyBuildRequest.UnitOrUpgradeType != unitTypeToProduce) {
            BuildRequests.Remove(_armyBuildRequest);

            _armyBuildRequest = new TargetBuildRequest(BuildType.Train, unitTypeToProduce, targetQuantity: 100, priority: BuildRequestPriority.Low);
            BuildRequests.Add(_armyBuildRequest);
        }

        // TODO GD Consider units in production too
        var ourForce = _warManager.ManagedUnits.GetForce();
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
    /// Returns the global enemy force
    /// </summary>
    /// <returns></returns>
    private static float GetEnemyForce() {
        // TODO GD Change EnemyMemorizedUnits to include all units that we know of
        return UnitsTracker.EnemyMemorizedUnits.Values.Concat(UnitsTracker.EnemyUnits).GetForce();
    }

    /// <summary>
    /// Determines if a rush is in progress by comparing our army to the enemy's
    /// </summary>
    /// <param name="ownArmy"></param>
    /// <returns></returns>
    private bool IsRushInProgress(IEnumerable<Unit> ownArmy) {
        var enemyForce = _startingRegions.Sum(region => RegionTracker.GetForce(region, Alliance.Enemy));
        var ownForce = ownArmy
            .Where(soldier => _startingRegions.Contains(soldier.GetRegion()))
            .GetForce();

        return enemyForce > ownForce;
    }

    /// <summary>
    /// Gets all eco units drafted to help defending
    /// </summary>
    /// <returns>All the eco units drafted to help defending</returns>
    private List<Unit> GetDraftedUnits() {
        return _warManager.ManagedUnits
            .Where(soldier => !ManageableUnitTypes.Contains(soldier.UnitType))
            .ToList();
    }
}
