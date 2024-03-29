﻿using System.Collections.Generic;
using System.Linq;
using Sajuuk.ExtensionMethods;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.Debugging;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.GameSense.RegionsEvaluationsTracking;
using Sajuuk.Managers.WarManagement.ArmySupervision;
using Sajuuk.Managers.WarManagement.States.MidGame;
using Sajuuk.MapAnalysis;
using Sajuuk.MapAnalysis.ExpandAnalysis;
using Sajuuk.MapAnalysis.RegionAnalysis;
using Sajuuk.Tagging;
using Sajuuk.UnitModules;
using SC2APIProtocol;

namespace Sajuuk.Managers.WarManagement.States.EarlyGame;

public class EarlyGameBehaviour : IWarManagerBehaviour {
    private static readonly HashSet<uint> ManageableUnitTypes = Units.ZergMilitary.Except(new HashSet<uint> { Units.Queen, Units.QueenBurrowed }).ToHashSet();

    private readonly EarlyGameBehaviourDebugger _debugger;
    private readonly WarManager _warManager;
    private readonly HashSet<IRegion> _startingRegions;

    private readonly ITaggingService _taggingService;
    private readonly IUnitsTracker _unitsTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IRegionsEvaluationsTracker _regionsEvaluationsTracker;
    private readonly IBuildRequestFactory _buildRequestFactory;
    private readonly TechTree _techTree;
    private readonly IController _controller;
    private readonly IUnitEvaluator _unitEvaluator;
    private readonly IPathfinder _pathfinder;

    private BuildRequest _armyBuildRequest;

    private bool _rushTagged = false;
    private bool _isRushInProgress = false;
    private bool _hasCleanUpStarted = false;

    public readonly ArmySupervisor DefenseSupervisor;

    public IAssigner Assigner { get; }
    public IDispatcher Dispatcher { get; }
    public IReleaser Releaser { get; }

    public List<BuildRequest> BuildRequests { get; } = new List<BuildRequest>();

    public EarlyGameBehaviour(
        WarManager warManager,
        ITaggingService taggingService,
        IDebuggingFlagsTracker debuggingFlagsTracker,
        IUnitsTracker unitsTracker,
        IRegionsTracker regionsTracker,
        IRegionsEvaluationsTracker regionsEvaluationsTracker,
        IWarSupervisorFactory warSupervisorFactory,
        IBuildRequestFactory buildRequestFactory,
        IGraphicalDebugger graphicalDebugger,
        TechTree techTree,
        IController controller,
        IUnitEvaluator unitEvaluator,
        IPathfinder pathfinder,
        IUnitModuleInstaller unitModuleInstaller
    ) {
        _warManager = warManager;
        _taggingService = taggingService;
        _unitsTracker = unitsTracker;
        _regionsTracker = regionsTracker;
        _regionsEvaluationsTracker = regionsEvaluationsTracker;
        _buildRequestFactory = buildRequestFactory;
        _techTree = techTree;
        _controller = controller;
        _unitEvaluator = unitEvaluator;
        _pathfinder = pathfinder;

        _debugger = new EarlyGameBehaviourDebugger(debuggingFlagsTracker, graphicalDebugger);
        DefenseSupervisor = warSupervisorFactory.CreateArmySupervisor();

        _armyBuildRequest = _buildRequestFactory.CreateTargetBuildRequest(BuildType.Train, Units.Roach, targetQuantity: 100, priority: BuildRequestPriority.Low);
        BuildRequests.Add(_armyBuildRequest);

        Assigner = new WarManagerAssigner<EarlyGameBehaviour>(unitModuleInstaller, this);
        Dispatcher = new EarlyGameDispatcher(this);
        Releaser = new WarManagerReleaser<EarlyGameBehaviour>(this);

        var main = _regionsTracker.GetRegion(_regionsTracker.GetExpand(Alliance.Self, ExpandType.Main).Position);
        var natural = _regionsTracker.GetRegion(_regionsTracker.GetExpand(Alliance.Self, ExpandType.Natural).Position);
        _startingRegions = _pathfinder.FindPath(main, natural).ToHashSet();
    }

    public void RecruitmentPhase() {
        if (_hasCleanUpStarted) {
            return;
        }

        _warManager.Assign(_unitsTracker.GetUnits(_unitsTracker.NewOwnedUnits, ManageableUnitTypes));
        RecruitEcoUnitsIfNecessary();
    }

    public void DispatchPhase() {
        if (_hasCleanUpStarted) {
            return;
        }

        _warManager.Dispatch(_warManager.ManagedUnits.Where(soldier => soldier.Supervisor == null));
    }

    public void ManagementPhase() {
        if (_hasCleanUpStarted) {
            return;
        }

        var regionToDefend = GetRegionToDefend();
        DefenseSupervisor.AssignTarget(regionToDefend.Center, regionToDefend.ApproximatedRadius, canHuntTheEnemy: false);
        DefenseSupervisor.OnFrame();

        AdjustBuildRequests();

        _debugger.OwnForce = _unitEvaluator.EvaluateForce(_warManager.ManagedUnits);
        _debugger.EnemyForce = GetEnemyForce();
        _debugger.Target = regionToDefend;
        _debugger.CurrentStance = Stance.Defend;
        _debugger.BuildPriority = BuildRequests.FirstOrDefault()?.Priority ?? BuildRequestPriority.Low;
        _debugger.BuildBlockCondition = BuildRequests.FirstOrDefault()?.BlockCondition ?? BuildBlockCondition.None;
        _debugger.Debug();
    }

    /// <summary>
    /// Release drafted eco units and clean up supervisors.
    /// </summary>
    /// <returns>True if the cleanup is complete, false if we need more time.</returns>
    public bool CleanUp() {
        if (_isRushInProgress) {
            return false;
        }

        _hasCleanUpStarted = true;

        var draftedUnits = GetDraftedUnits();
        if (draftedUnits.Any()) {
            _warManager.Release(draftedUnits);

            // We give one tick so that release orders, like stop or unburrow go through
            return false;
        }

        if (DefenseSupervisor.SupervisedUnits.Any()) {
            DefenseSupervisor.Retire();

            // We give one tick so that release orders, like stop or unburrow go through
            return false;
        }

        return true;
    }

    private IRegion GetRegionToDefend() {
        if (GetEnemyForce(_startingRegions) == 0) {
            var enemyMain = _regionsTracker.GetRegion(_regionsTracker.GetExpand(Alliance.Enemy, ExpandType.Main).Position);
            return _startingRegions.MinBy(region => _pathfinder.FindPath(region, enemyMain).GetPathDistance());
        }

        return _startingRegions.MaxBy(region => _regionsEvaluationsTracker.GetForce(region, Alliance.Enemy))!;
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
            _taggingService.TagEarlyAttack();
            _rushTagged = true;
        }

        var townHallSupervisors = _unitsTracker
            .GetUnits(_unitsTracker.OwnedUnits, Units.TownHalls)
            .Where(unit => unit.Supervisor != null)
            .Select(supervisedTownHall => supervisedTownHall.Supervisor)
            .ToList();

        var draftableDrones = new List<Unit>();
        foreach (var townHallSupervisor in townHallSupervisors) {
            _warManager.Assign(_unitsTracker.GetUnits(townHallSupervisor.SupervisedUnits, Units.Queen));

            draftableDrones
                .AddRange(_unitsTracker.GetUnits(townHallSupervisor.SupervisedUnits, Units.Drone)
                // TODO GD Maybe sometimes we should take all of them
                .Skip(2));
        }

        draftableDrones = draftableDrones
            .OrderByDescending(drone => drone.Integrity)
            // TODO GD This could be better, it assumes the threat comes from the natural
            .ThenBy(drone => drone.DistanceTo(_regionsTracker.GetExpand(Alliance.Self, ExpandType.Natural).Position))
            .ToList();

        var enemyForce = GetEnemyForce();
        var draftIndex = 0;
        while (draftIndex < draftableDrones.Count && _unitEvaluator.EvaluateForce(_warManager.ManagedUnits, areWorkersOffensive: true) < enemyForce) {
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

            _armyBuildRequest = _buildRequestFactory.CreateTargetBuildRequest(BuildType.Train, unitTypeToProduce, targetQuantity: 100, priority: BuildRequestPriority.Low);
            BuildRequests.Add(_armyBuildRequest);
        }

        // TODO GD Consider units in production too
        var ourForce = _unitEvaluator.EvaluateForce(_warManager.ManagedUnits);
        // TODO GD Exclude buildings?
        var enemyForce = GetEnemyForce();

        // This is a small tweak to prevent blocking the build when getting scouted (LOL)
        // TODO GD We need a way to tell that an enemy drone is hostile to better handle this
        if (enemyForce <= 1) {
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
    private uint GetUnitTypeToProduce() {
        if (_controller.IsUnlocked(Units.Roach, _techTree.UnitPrerequisites)) {
            return Units.Roach;
        }

        if (_unitsTracker.GetUnits(_unitsTracker.OwnedUnits, Units.SpawningPool).Any()) {
            return Units.Zergling;
        }

        // TODO GD Not sure if this is good
        return Units.Drone;
    }

    /// <summary>
    /// Returns the enemy force, filtering by the provided regions, if any.
    /// </summary>
    /// <returns>The enemy force</returns>
    private float GetEnemyForce(IReadOnlySet<IRegion> regionsFilter = null) {
        var enemyUnits = _unitsTracker.EnemyMemorizedUnits.Values.Concat(_unitsTracker.EnemyUnits);
        if (regionsFilter != null) {
            enemyUnits = enemyUnits.Where(enemy => regionsFilter.Contains(enemy.GetRegion()));
        }

        return _unitEvaluator.EvaluateForce(enemyUnits);
    }

    /// <summary>
    /// Determines if a rush is in progress by comparing our army to the enemy's
    /// </summary>
    /// <param name="ownArmy"></param>
    /// <returns></returns>
    private bool IsRushInProgress(IEnumerable<Unit> ownArmy) {
        var enemyForce = GetEnemyForce(_startingRegions);
        var ownForce = _unitEvaluator.EvaluateForce(ownArmy.Where(soldier => _startingRegions.Contains(soldier.GetRegion())));

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
