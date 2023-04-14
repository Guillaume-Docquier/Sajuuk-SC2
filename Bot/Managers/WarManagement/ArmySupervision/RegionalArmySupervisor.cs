using System.Collections.Generic;
using System.Linq;
using Bot.Algorithms;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement.ArmySupervision;

public class RegionalArmySupervisor : Supervisor {
    private readonly IUnitsControl _unitsController = new UnitsController();
    private readonly IRegion _targetRegion;

    // TODO GD Rework assigner/releaser. It's not helpful at all
    protected override IAssigner Assigner { get; } = new DummyAssigner();
    protected override IReleaser Releaser { get; } = new DummyReleaser();

    public override IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();

    public RegionalArmySupervisor(IRegion targetRegion) {
        _targetRegion = targetRegion;
    }

    protected override void Supervise() {
        if (!SupervisedUnits.Any()) {
            return;
        }

        var approachRegions = _targetRegion.GetReachableNeighbors().ToHashSet();
        // TODO GD Sometimes, units are in position outside of approach regions because they are avoiding the enemy. The should be included
        var unitsInPosition = GetUnitsInPosition(SupervisedUnits, _targetRegion, approachRegions);
        var enemyArmy = GetEnemyArmy(_targetRegion).ToList();

        if (unitsInPosition.GetForce() >= enemyArmy.GetForce()) {
            Attack(unitsInPosition, _targetRegion, enemyArmy);
            MoveIntoPosition(SupervisedUnits.Except(unitsInPosition).ToList(), approachRegions, enemyArmy);
        }
        else {
            MoveIntoPosition(SupervisedUnits, approachRegions, enemyArmy);
        }
    }

    /// <summary>
    /// Attacks the target region.
    /// </summary>
    /// <param name="units">The units that must attack</param>
    /// <param name="targetRegion">The region to attack</param>
    /// <param name="enemyArmy">The enemy units to engage</param>
    private void Attack(IReadOnlySet<Unit> units, IRegion targetRegion, IReadOnlyCollection<Unit> enemyArmy) {
        // TODO GD We can improve target selection
        var target = targetRegion.Center;
        if (enemyArmy.Any()) {
            target = enemyArmy.MinBy(unit => unit.DistanceTo(targetRegion.Center)).Position.ToVector2();
        }

        var unhandledUnits = _unitsController.Execute(units);
        foreach (var unhandledUnit in unhandledUnits) {
            unhandledUnit.AttackMove(target);
        }
    }

    /// <summary>
    /// Moves units into strike range by using the given approach regions.
    /// Units will avoid engaging the enemy.
    /// </summary>
    /// <param name="units">The units to move</param>
    /// <param name="approachRegions">The regions in strike range of the global objective</param>
    /// <param name="enemyArmy">The enemy units to get in range of but avoid engaging</param>
    private static void MoveIntoPosition(IReadOnlyCollection<Unit> units, IReadOnlyCollection<IRegion> approachRegions, IReadOnlyCollection<Unit> enemyArmy) {
        var regionsOutOfReach = ComputeBlockedRegionsMap(units);

        var unitGroups = units
            .Where(unit => unit.GetRegion() != null)
            .GroupBy(unit => approachRegions.MinBy(approachRegion => {
                var unitRegion = unit.GetRegion();
                var blockedRegions = regionsOutOfReach[unitRegion];
                if (blockedRegions.Contains(approachRegion)) {
                    return float.MaxValue;
                }

                return Pathfinder.FindPath(unitRegion, approachRegion, blockedRegions).GetPathDistance();
            }));

        foreach (var unitGroup in unitGroups) {
            MoveTowards(unitGroup, unitGroup.Key, regionsOutOfReach, enemyArmy);
        }
    }

    /// <summary>
    /// Moves towards the target region by following a path that avoids certain regions.
    /// Units will avoid engaging the enemy.
    /// </summary>
    /// <param name="units">The units to move</param>
    /// <param name="targetRegion">The region to go to</param>
    /// <param name="blockedRegions">The regions to avoid going through</param>
    /// <param name="enemyArmy">The enemy units to get in range of but avoid engaging</param>
    private static void MoveTowards(IEnumerable<Unit> units, IRegion targetRegion, IDictionary<IRegion, HashSet<IRegion>> blockedRegions, IReadOnlyCollection<Unit> enemyArmy) {
        foreach (var unit in units) {
            var closestEnemy = enemyArmy.MinBy(enemy => enemy.DistanceTo(unit) - enemy.MaxRange);
            if (closestEnemy != null && closestEnemy.DistanceTo(unit) < closestEnemy.MaxRange * 2) {
                // TODO GD We should avoid cornering ourselves, maybe we should go towards a region exit?
                unit.MoveAwayFrom(closestEnemy.Position.ToVector2(), closestEnemy.MaxRange * 2);
                continue;
            }

            var unitRegion = unit.GetRegion();

            if (unitRegion == targetRegion) {
                unit.Move(targetRegion.Center);
                continue;
            }

            var path = Pathfinder.FindPath(unitRegion, targetRegion, blockedRegions[unitRegion]);
            if (path == null) {
                // Trying to gracefully handle a case that I don't think should happen
                unit.Move(targetRegion.Center);
                continue;
            }

            var nextRegion = path
                .Skip(1)
                .First();

            unit.Move(nextRegion.Center);
        }
    }

    /// <summary>
    /// Gets all the units that are in position and ready to attack the target region.
    /// </summary>
    /// <param name="supervisedUnits">The units to consider</param>
    /// <param name="targetRegion"></param>
    /// <param name="approachRegions"></param>
    /// <returns></returns>
    private static HashSet<Unit> GetUnitsInPosition(IEnumerable<Unit> supervisedUnits, IRegion targetRegion, IReadOnlySet<IRegion> approachRegions) {
        return supervisedUnits
            .Where(unit => {
                var unitRegion = unit.GetRegion();

                return unitRegion == targetRegion || approachRegions.Contains(unitRegion);
            })
            .ToHashSet();
    }

    /// <summary>
    /// Gets the enemy units that need to be defeated.
    /// This includes all units that are in a cluster where one member is in the target region.
    /// </summary>
    /// <param name="targetRegion">The target region.</param>
    /// <returns>The enemy units to defeat</returns>
    private static IEnumerable<Unit> GetEnemyArmy(IRegion targetRegion) {
        var enemies = UnitsTracker.EnemyUnits
            .Concat(UnitsTracker.EnemyGhostUnits.Values)
            .Where(enemy => !enemy.IsFlying) // TODO GD Bad bad hardcode
            .ToList();

        var clusteringResult = Clustering.DBSCAN(enemies, 2, 3);

        return clusteringResult.clusters
            .Where(cluster => cluster.Any(unit => unit.GetRegion() == targetRegion))
            .SelectMany(cluster => cluster)
            .Concat(clusteringResult.noise.Where(unit => unit.GetRegion() == targetRegion));
    }

    public override void Retire() {
        foreach (var supervisedUnit in SupervisedUnits) {
            Release(supervisedUnit);
        }
    }

    public IEnumerable<Unit> GetReleasableUnits() {
        // TODO GD Implement this for real
        // Units not necessary to a current fight can be released
        return SupervisedUnits;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="units"></param>
    /// <returns></returns>
    private static IDictionary<IRegion, HashSet<IRegion>> ComputeBlockedRegionsMap(IEnumerable<Unit> units) {
        var regionsWithFriendlyUnitPresence = units
            .Select(unit => unit.GetRegion())
            .Where(region => region != null)
            .ToHashSet();

        var reachableRegions = ComputeRegionsReach(regionsWithFriendlyUnitPresence);

        return regionsWithFriendlyUnitPresence.ToDictionary(
            region => region,
            region => RegionAnalyzer.Regions.Except(reachableRegions[region]).ToHashSet()
        );
    }

    /// <summary>
    /// Computes the reach of each of the provided region.
    /// A region is reachable if there's a path to it that doesn't go through a dangerous region.
    /// </summary>
    /// <param name="regions">The regions to compute the reach of.</param>
    /// <returns></returns>
    // TODO GD Share this instead of recomputing
    private static Dictionary<IRegion, List<IRegion>> ComputeRegionsReach(IEnumerable<IRegion> regions) {
        var reach = new Dictionary<IRegion, List<IRegion>>();
        foreach (var startingRegion in regions) {
            // TODO GD We can greatly optimize this by using dynamic programming
            reach[startingRegion] = TreeSearch.BreadthFirstSearch(
                startingRegion,
                region => region.GetReachableNeighbors().Where(neighbor => RegionTracker.GetForce(neighbor, Alliance.Enemy) <= 0),
                _ => false // TODO GD We might not need this predicate after all?
            ).ToList();
        }

        return reach;
    }

    private class DummyAssigner : IAssigner { public void Assign(Unit unit) {} }
    private class DummyReleaser : IReleaser { public void Release(Unit unit) {} }
}
