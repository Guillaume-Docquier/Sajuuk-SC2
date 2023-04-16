using System;
using System.Collections.Generic;
using System.Linq;
using Bot.Algorithms;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
using Bot.Managers.ScoutManagement.ScoutingSupervision;
using Bot.Managers.ScoutManagement.ScoutingTasks;
using Bot.Managers.WarManagement.ArmySupervision.RegionalArmySupervision;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement.States.MidGame;

/*
 * Ok, we're changing this
 * The war manager does not decide what is dangerous
 * It uses danger values that other systems can tweak (i.e predicting future threats)
 * The war manager just allocates the army to respond to the perceived threats (but it doesn't perceive them!)
 *
 * If we're stronger, attack the enemy head on
 * - Dispatch enough forces to handle threats in decreasing order
 * If we're weaker, contain the enemy
 * - Dispatch enough forces to handle threats in increasing order?
 *
 * If we have extra forces, dispatch harass groups
 *
 * We'll have to tweak what 'force' is
 * We'll have to include the minerals/gas to consider value
 *
 * Concepts
 * Army Strength: How strong an army is. This is almost purely related to army composition and size
 * Army Threat: How much damage the army is about to cause. A strong army stuck in a corner is not dangerous
 * Eco Damage: How much Non-army stuff is worth. This can be used to evaluate an Army Threat or the Unit Impact of a counter attack
 * Goal Value: The combination of army threat and eco damage
 * Unit Impact: The combination of goal value and distance
 */

public class MidGameBehaviour : IWarManagerBehaviour {
    private static readonly Random Rng = new Random();
    private static readonly HashSet<uint> ManageableUnitTypes = Units.ZergMilitary.Except(new HashSet<uint> { Units.Queen, Units.QueenBurrowed }).ToHashSet();

    private readonly MidGameBehaviourDebugger _debugger = new MidGameBehaviourDebugger();
    private readonly WarManager _warManager;
    private readonly Dictionary<IRegion, RegionalArmySupervisor> _armySupervisors;
    private readonly HashSet<ScoutSupervisor> _scoutSupervisors = new HashSet<ScoutSupervisor>();

    private BuildRequest _armyBuildRequest = new TargetBuildRequest(BuildType.Train, Units.Roach, targetQuantity: 100, priority: BuildRequestPriority.Low);
    private bool _hasCleanUpStarted = false;

    public IAssigner Assigner { get; }
    public IDispatcher Dispatcher { get; }
    public IReleaser Releaser { get; }

    public List<BuildRequest> BuildRequests { get; } = new List<BuildRequest>();

    public MidGameBehaviour(WarManager warManager) {
        _warManager = warManager;
        _armySupervisors = RegionAnalyzer.Regions.ToDictionary(region => region as IRegion, region => new RegionalArmySupervisor(region));

        BuildRequests.Add(_armyBuildRequest);

        Assigner = new WarManagerAssigner<MidGameBehaviour>(this);

        // TODO GD We don't need that shit in all managers
        Dispatcher = new MidGameDispatcher(this);

        Releaser = new WarManagerReleaser<MidGameBehaviour>(this);
    }

    public void RecruitmentPhase() {
        if (_hasCleanUpStarted) {
            return;
        }

        _warManager.Assign(Controller.GetUnits(UnitsTracker.NewOwnedUnits, ManageableUnitTypes));
    }

    public void DispatchPhase() {
        if (_hasCleanUpStarted) {
            return;
        }

        if (!_armySupervisors.Any(kv => IsAGoal(kv.Key))) {
            Scout();

            return;
        }

        CancelScouting();

        var plannedUnitsAllocation = PlanUnitsAllocationToMaximizeImpact(GetAvailableUnits());
        // TODO GD Reassign released units until they're all assigned?
        // ReleaseUnitsFromUnachievableGoals(plannedUnitsAllocation);
        PerformAllocation(plannedUnitsAllocation);

        RecallUnsupervisedUnits();
    }

    public void ManagementPhase() {
        if (_hasCleanUpStarted) {
            return;
        }

        // TODO GD We need a way to tell some supervisors to disengage because we want their units

        AdjustBuildRequests();

        foreach (var supervisor in _armySupervisors.Values) {
            supervisor.OnFrame();
        }

        foreach (var supervisor in _scoutSupervisors.ToList()) {
            if (supervisor.ScoutingTask.IsComplete()) {
                supervisor.Retire();
                _scoutSupervisors.Remove(supervisor);
            }
            else {
                supervisor.OnFrame();
            }
        }

        // TODO GD The war manager could do most of that
        _debugger.OwnForce = _warManager.ManagedUnits.GetForce();
        _debugger.EnemyForce = GetTotalEnemyForce();
        _debugger.BuildPriority = BuildRequests.FirstOrDefault()?.Priority ?? BuildRequestPriority.Low;
        _debugger.BuildBlockCondition = BuildRequests.FirstOrDefault()?.BlockCondition ?? BuildBlockCondition.None;
        _debugger.Debug();
    }

    public bool CleanUp() {
        _hasCleanUpStarted = true;

        if (_warManager.ManagedUnits.All(unit => unit.Supervisor == null)) {
            _scoutSupervisors.Clear();

            return true;
        }

        foreach (var supervisor in _armySupervisors.Values) {
            supervisor.Retire();
        }

        foreach (var supervisor in _scoutSupervisors) {
            supervisor.Retire();
        }

        // Retire might take a few frames
        return false;
    }

    private void Scout() {
        if (!_scoutSupervisors.Any()) {
            foreach (var (_, armySupervisor) in _armySupervisors) {
                armySupervisor.Retire();
            }

            InitializeScoutingTasks();
        }

        AssignScouts();
        RecallUnsupervisedUnits();
    }

    private void InitializeScoutingTasks() {
        var expandsToScout = ExpandAnalyzer.ExpandLocations
            .Where(expandLocation => !VisibilityTracker.IsVisible(expandLocation.Position));

        foreach (var expandToScout in expandsToScout) {
            var scoutingTask = new ExpandScoutingTask(expandToScout.Position, priority: 0, maxScouts: 1);
            var scoutingSupervisor = new ScoutSupervisor(scoutingTask);

            _scoutSupervisors.Add(scoutingSupervisor);
        }
    }

    private void AssignScouts() {
        var unsupervisedUnits = _warManager.ManagedUnits
            .Where(unit => unit.Supervisor is not ScoutSupervisor) // TODO GD Does that stink?
            .ToHashSet();

        // TODO GD Improve the scout supervisor api, we should not need to use the task like that
        foreach (var scoutingSupervisor in _scoutSupervisors.Where(supervisor => supervisor.SupervisedUnits.Count < supervisor.ScoutingTask.MaxScouts)) {
            var scout = unsupervisedUnits.MinBy(unit => unit.DistanceTo(scoutingSupervisor.ScoutingTask.ScoutLocation));

            if (scout == null) {
                // No more unsupervised units
                return;
            }

            scoutingSupervisor.Assign(scout);
            unsupervisedUnits.Remove(scout);
        }

        // Randomly distribute unsupervised units to scouting tasks based on distances.
        // A close task will have a higher chance to be picked than a far task.
        // The intuition is that this method, while not exact, will more or less distribute units properly.
        // However, it is fast to compute because it requires a single pass on the unit list.
        foreach (var unsupervisedUnit in unsupervisedUnits) {
            var assignmentDistances = _scoutSupervisors
                .Select(supervisor => (supervisor, distance: supervisor.ScoutingTask.ScoutLocation.DistanceTo(unsupervisedUnit)))
                .ToList();

            var totalDistance = assignmentDistances.Sum(assignment => assignment.distance);

            var assignmentProbabilities = assignmentDistances
                .Select(assignment => (assignment.supervisor, probability: 1f - assignment.distance / totalDistance))
                .ToList();

            // Weighted random implementation
            var roll = Rng.NextSingle();
            var lowBound = 1f;
            foreach (var (supervisor, probability) in assignmentProbabilities) {
                lowBound -= probability;

                if (roll >= lowBound) {
                    supervisor.Assign(unsupervisedUnit);
                    return;
                }
            }

            // Just in case a rounding error occurred
            assignmentProbabilities[^1].supervisor.Assign(unsupervisedUnit);
        }
    }

    private void CancelScouting() {
        foreach (var scoutSupervisor in _scoutSupervisors) {
            scoutSupervisor.ScoutingTask.Cancel();
            scoutSupervisor.Retire();
        }

        _scoutSupervisors.Clear();
    }

    private static Dictionary<IRegion, List<Unit>> PlanUnitsAllocationToMaximizeImpact(IEnumerable<Unit> availableUnits) {
        var regionsReach = ComputeRegionsReach(RegionAnalyzer.Regions);

        var allocations = RegionAnalyzer.Regions.ToDictionary(region => region as IRegion, _ => new List<Unit>());
        foreach (var availableUnit in availableUnits.Where(unit => unit.GetRegion() != null)) {
            var mostImpactfulRegion = GetMostImpactfulRegion(availableUnit, regionsReach[availableUnit.GetRegion()]);
            allocations[mostImpactfulRegion].Add(availableUnit);
        }

        return allocations;
    }

    /// <summary>
    /// Gets all units that can be reassigned right now.
    /// </summary>
    /// <returns>All the units that can be reassigned.</returns>
    private IEnumerable<Unit> GetAvailableUnits() {
        return _armySupervisors.Values
            .Where(supervisor => supervisor.SupervisedUnits.Any())
            .SelectMany(supervisor => supervisor.GetReleasableUnits())
            .Concat(_warManager.ManagedUnits.Where(unit => unit.Supervisor == null));
    }

    /// <summary>
    /// Get the region from reachableRegions where the given unit will have to most impact.
    /// </summary>
    /// <param name="unit">The unit that wants to have an impact.</param>
    /// <param name="reachableRegions">The regions where the unit is allowed to have an impact.</param>
    /// <returns>The region from reachableRegions where the given unit will have to most impact.</returns>
    private static IRegion GetMostImpactfulRegion(Unit unit, IReadOnlyCollection<IRegion> reachableRegions) {
        var unitRegion = unit.GetRegion();
        var regionsToAvoid = RegionAnalyzer.Regions.Except(reachableRegions).ToHashSet();

        return reachableRegions.MaxBy(reachableRegion => {
            var regionThreat = RegionTracker.GetThreat(reachableRegion, Alliance.Enemy);
            var regionValue = RegionTracker.GetValue(reachableRegion, Alliance.Enemy);

            var distance = Pathfinder.FindPath(unitRegion, reachableRegion, regionsToAvoid).GetPathDistance();

            return (regionThreat + regionValue) / (distance + 1f); // Add 1 to ensure non zero division
        });
    }

    private static void ReleaseUnitsFromUnachievableGoals(Dictionary<IRegion, List<Unit>> plannedUnitsAllocation) {
        foreach (var (region, army) in plannedUnitsAllocation) {
            var hasEnoughForce = army.GetForce() >= RegionTracker.GetForce(region, Alliance.Enemy) * 2;
            if (IsAGoal(region) && hasEnoughForce) {
                continue;
            }

            plannedUnitsAllocation.Remove(region);
        }
    }

    private static bool IsAGoal(IRegion region) {
        var thereIsAThreat = RegionTracker.GetThreat(region, Alliance.Enemy) > 0;
        var thereIsValue = RegionTracker.GetValue(region, Alliance.Enemy) > 0;

        return thereIsAThreat || thereIsValue;
    }

    private void PerformAllocation(Dictionary<IRegion, List<Unit>> plannedUnitsAllocation) {
        foreach (var (region, army) in plannedUnitsAllocation) {
            _armySupervisors[region].Assign(army);
        }
    }

    private void RecallUnsupervisedUnits() {
        var safeRegions = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.TownHalls)
            .Select(townHall => townHall.GetRegion())
            .ToHashSet();

        var ourMainRegion = ExpandAnalyzer.GetExpand(Alliance.Self, ExpandType.Main).GetRegion();
        var enemyMainRegion = ExpandAnalyzer.GetExpand(Alliance.Enemy, ExpandType.Main).GetRegion();
        foreach (var unsupervisedUnit in _warManager.ManagedUnits.Where(unit => unit.Supervisor == null)) {
            // TODO GD We should make sure that units have a region, this is error prone
            var unitRegion = unsupervisedUnit.GetRegion();
            if (unitRegion == null) {
                unsupervisedUnit.Move(ourMainRegion.Center);
                continue;
            }

            var regionToGoTo = safeRegions.MinBy(safeRegion => {
                var safeToDangerDistance = Pathfinder.FindPath(safeRegion, enemyMainRegion).GetPathDistance();
                var unitToSafeDistance =  Pathfinder.FindPath(unitRegion, safeRegion).GetPathDistance();

                return unitToSafeDistance + safeToDangerDistance;
            });

            _armySupervisors[regionToGoTo].Assign(unsupervisedUnit);
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
        var enemyForce = GetTotalEnemyForce();

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

        // This will include spawning pool in progress. We'll want to start saving larvae for drones
        if (Controller.GetUnits(UnitsTracker.OwnedUnits, Units.SpawningPool).Any()) {
            return Units.Zergling;
        }

        // TODO GD Not sure if this is good
        return Units.Drone;
    }

    /// <summary>
    /// Returns the total enemy force.
    /// </summary>
    /// <returns>The total enemy force.</returns>
    private static float GetTotalEnemyForce() {
        return UnitsTracker.EnemyMemorizedUnits.Values.Concat(UnitsTracker.EnemyUnits).GetForce();
    }

    /// <summary>
    /// Computes the reach of each of the provided region.
    /// A region is reachable if there's a path to it that doesn't go through a dangerous region.
    /// </summary>
    /// <param name="regions">The regions to compute the reach of.</param>
    /// <returns></returns>
    private static Dictionary<IRegion, List<IRegion>> ComputeRegionsReach(IEnumerable<IRegion> regions) {
        var reach = new Dictionary<IRegion, List<IRegion>>();
        foreach (var startingRegion in regions) {
            // TODO GD We can greatly optimize this by using dynamic programming
            reach[startingRegion] = TreeSearch.BreadthFirstSearch(
                startingRegion,
                region => region.GetReachableNeighbors(),
                region => RegionTracker.GetForce(region, Alliance.Enemy) > 0
            ).ToList();
        }

        return reach;
    }

    // We don't use it
    private class MidGameDispatcher : Dispatcher<MidGameBehaviour> {
        public MidGameDispatcher(MidGameBehaviour client) : base(client) {}

        public override void Dispatch(Unit unit) {}
    }
}
