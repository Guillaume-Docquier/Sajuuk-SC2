using System.Collections.Generic;
using System.Linq;
using Bot.Algorithms;
using Bot.Builds;
using Bot.ExtensionMethods;
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

    // TODO Use clustering to determine the real size of enemy threat even if they're outside the assigned region
    protected override void Supervise() {
        if (!SupervisedUnits.Any()) {
            return;
        }

        var regionsWithFriendlyUnitPresence = SupervisedUnits
            .Select(unit => unit.GetRegion())
            .Where(region => region != null)
            .ToHashSet();

        var approachRegions = _targetRegion.GetReachableNeighbors().ToHashSet();
        var unitsInPosition = GetUnitsInPosition(SupervisedUnits, _targetRegion, approachRegions);

        var assignedRegionForce = RegionTracker.GetForce(_targetRegion, Alliance.Enemy);
        if (unitsInPosition.GetForce() >= assignedRegionForce) {
            Attack(unitsInPosition, _targetRegion);
            MoveIntoPosition(SupervisedUnits.Except(unitsInPosition), approachRegions, regionsWithFriendlyUnitPresence);
        }
        else {
            MoveIntoPosition(SupervisedUnits, approachRegions, regionsWithFriendlyUnitPresence);
        }
    }

    private void Attack(IReadOnlySet<Unit> units, IRegion targetRegion) {
        // TODO GD We can improve target selection
        var target = targetRegion.Center;
        var targetUnits = UnitsTracker.EnemyUnits
            .Concat(UnitsTracker.EnemyGhostUnits.Values)
            .Where(unit => !unit.IsFlying) // TODO GD Bad hardcode, lazy me
            .Where(unit => unit.GetRegion() == targetRegion)
            .ToList();

        if (targetUnits.Any()) {
            target = targetUnits.MinBy(unit => unit.DistanceTo(targetRegion.Center)).Position.ToVector2();
        }

        var unhandledUnits = _unitsController.Execute(units);
        foreach (var unhandledUnit in unhandledUnits) {
            unhandledUnit.AttackMove(target);
        }
    }

    private static void MoveIntoPosition(IEnumerable<Unit> units, IReadOnlyCollection<IRegion> approachRegions, IReadOnlyCollection<IRegion> regionsWithFriendlyUnitPresence) {
        var reachableRegions = ComputeRegionsReach(regionsWithFriendlyUnitPresence);
        var regionsOutOfReach = regionsWithFriendlyUnitPresence.ToDictionary(
            region => region,
            region => RegionAnalyzer.Regions.Except(reachableRegions[region]).ToHashSet()
        );

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
            MoveTowards(unitGroup.Key, unitGroup, regionsOutOfReach);
        }
    }

    private static void MoveTowards(IRegion targetRegion, IEnumerable<Unit> units, IDictionary<IRegion, HashSet<IRegion>> blockedRegions) {
        foreach (var unit in units) {
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

    private static HashSet<Unit> GetUnitsInPosition(IEnumerable<Unit> supervisedUnits, IRegion targetRegion, IReadOnlySet<IRegion> approachRegions) {
        return supervisedUnits
            .Where(unit => {
                var unitRegion = unit.GetRegion();

                return unitRegion == targetRegion || approachRegions.Contains(unitRegion);
            })
            .ToHashSet();
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
                region => region.GetReachableNeighbors(),
                region => region != startingRegion && RegionTracker.GetForce(region, Alliance.Enemy) > 0
            ).ToList();
        }

        return reach;
    }

    private class DummyAssigner : IAssigner { public void Assign(Unit unit) {} }
    private class DummyReleaser : IReleaser { public void Release(Unit unit) {} }
}
