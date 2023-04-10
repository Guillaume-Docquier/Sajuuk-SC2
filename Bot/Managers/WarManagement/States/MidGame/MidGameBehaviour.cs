using System.Collections.Generic;
using System.Linq;
using Bot.Algorithms;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
using Bot.Managers.WarManagement.ArmySupervision;
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
    private static readonly HashSet<uint> ManageableUnitTypes = Units.ZergMilitary.Except(new HashSet<uint> { Units.Queen, Units.QueenBurrowed }).ToHashSet();

    private readonly MidGameBehaviourDebugger _debugger = new MidGameBehaviourDebugger();
    private readonly WarManager _warManager;
    private readonly Dictionary<Region, RegionalArmySupervisor> _supervisors;

    private BuildRequest _armyBuildRequest = new TargetBuildRequest(BuildType.Train, Units.Roach, targetQuantity: 100, priority: BuildRequestPriority.Low);
    private bool _hasCleanUpStarted = false;

    public IAssigner Assigner { get; }
    public IDispatcher Dispatcher { get; }
    public IReleaser Releaser { get; }

    public List<BuildRequest> BuildRequests { get; } = new List<BuildRequest>();

    public MidGameBehaviour(WarManager warManager) {
        _warManager = warManager;
        _supervisors = RegionAnalyzer.Regions.ToDictionary(region => region, region => new RegionalArmySupervisor(region));

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

        var regionsReach = ComputeRegionsReach(RegionAnalyzer.Regions);

        // TODO GD Do two (or more?) rounds to cancel goals that can't be achieved. Reassign to achievable goals
        // TODO GD Don't assign yet?
        foreach (var availableUnit in GetAvailableUnits()) {
            var mostImpactfulRegion = GetMostImpactfulRegion(availableUnit, regionsReach[availableUnit.GetRegion()]);
            _supervisors[mostImpactfulRegion].Assign(availableUnit);
        }
    }

    public void ManagementPhase() {
        if (_hasCleanUpStarted) {
            return;
        }

        // TODO GD We need a way to tell some supervisors to disengage because we want their units

        AdjustBuildRequests();

        foreach (var supervisor in _supervisors.Values) {
            supervisor.OnFrame();
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

        if (!_supervisors.Values.Any(supervisor => supervisor.SupervisedUnits.Any())) {
            return true;
        }

        foreach (var supervisor in _supervisors.Values) {
            supervisor.Retire();
        }

        // Retire might take a few frames
        return false;
    }

    /// <summary>
    /// Gets all units that can be reassigned right now.
    /// </summary>
    /// <returns>All the units that can be reassigned.</returns>
    private IEnumerable<Unit> GetAvailableUnits() {
        return _supervisors.Values
            .SelectMany(supervisor => supervisor.GetReleasableUnits())
            .Concat(_warManager.ManagedUnits.Where(unit => unit.Supervisor == null));
    }

    /// <summary>
    /// Get the region from reachableRegions where the given unit will have to most impact.
    /// </summary>
    /// <param name="unit">The unit that wants to have an impact.</param>
    /// <param name="reachableRegions">The regions where the unit is allowed to have an impact.</param>
    /// <returns>The region from reachableRegions where the given unit will have to most impact.</returns>
    private static Region GetMostImpactfulRegion(Unit unit, IReadOnlyCollection<Region> reachableRegions) {
        var unitRegion = unit.GetRegion();
        var regionsToAvoid = RegionAnalyzer.Regions.Except(reachableRegions).ToHashSet();

        return reachableRegions.MaxBy(reachableRegion => {
            var regionThreat = RegionTracker.GetThreat(reachableRegion, Alliance.Enemy);
            var regionValue = RegionTracker.GetValue(reachableRegion, Alliance.Enemy);

            var distance = Pathfinder.FindPath(unitRegion, reachableRegion, regionsToAvoid).GetPathDistance();

            return (regionThreat + regionValue) / (distance + 1f); // Add 1 to ensure non zero division
        });
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
    private static Dictionary<Region, List<Region>> ComputeRegionsReach(List<Region> regions) {
        var reach = new Dictionary<Region, List<Region>>();
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
