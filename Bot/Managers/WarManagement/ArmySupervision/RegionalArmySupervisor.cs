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
    private readonly IRegion _assignedRegion;

    // TODO GD Rework assigner/releaser. It's not helpful at all
    protected override IAssigner Assigner { get; } = new DummyAssigner();
    protected override IReleaser Releaser { get; } = new DummyReleaser();

    public override IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();

    public RegionalArmySupervisor(IRegion assignedRegion) {
        _assignedRegion = assignedRegion;
    }

    protected override void Supervise() {
        if (!SupervisedUnits.Any()) {
            return;
        }

        var assignedRegionForce = RegionTracker.GetForce(_assignedRegion, Alliance.Enemy);
        var regionsWithUnitPresence = SupervisedUnits
            .Select(unit => unit.GetRegion())
            .Where(region => region != null)
            .ToHashSet();

        var targetRegions = _assignedRegion.GetReachableNeighbors().ToHashSet();
        var unitsInPosition = SupervisedUnits
            .Where(unit => {
                var unitRegion = unit.GetRegion();

                return unitRegion == _assignedRegion || targetRegions.Contains(unitRegion);
            })
            .ToHashSet();

        if (unitsInPosition.GetForce() >= assignedRegionForce) {
            Attack(_assignedRegion, unitsInPosition);
            MoveIntoPosition(regionsWithUnitPresence, targetRegions, SupervisedUnits.Except(unitsInPosition));
        }
        else {
            MoveIntoPosition(regionsWithUnitPresence, targetRegions, SupervisedUnits);
        }
    }

    private void Attack(IRegion targetRegion, IReadOnlySet<Unit> units) {
        // TODO GD We can improve target selection
        var target = targetRegion.Center;
        var targetUnits = UnitsTracker.EnemyUnits
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

    private void MoveIntoPosition(IReadOnlyCollection<IRegion> regionsWithUnitPresence, IReadOnlyCollection<IRegion> targetRegions, IEnumerable<Unit> units) {
        var reachableRegions = ComputeRegionsReach(regionsWithUnitPresence);
        var regionsOutOfReach = regionsWithUnitPresence.ToDictionary(
            region => region,
            region => RegionAnalyzer.Regions.Except(reachableRegions[region]).ToHashSet()
        );

        var unitGroups = units
            .Where(unit => unit.GetRegion() != null)
            .GroupBy(unit => targetRegions.MinBy(targetRegion => {
                var unitRegion = unit.GetRegion();
                var regionsToAvoid = regionsOutOfReach[unitRegion];
                if (regionsToAvoid.Contains(targetRegion)) {
                    return float.MaxValue;
                }

                return Pathfinder.FindPath(unitRegion, targetRegion, regionsToAvoid).GetPathDistance();
            }));

        foreach (var unitGroup in unitGroups) {
            var unhandledUnits = _unitsController.Execute(unitGroup.ToHashSet());
            foreach (var unhandledUnit in unhandledUnits) {
                var unitRegion = unhandledUnit.GetRegion();
                var regionsToAvoid = regionsOutOfReach[unitRegion];

                if (unitRegion == unitGroup.Key) {
                    unhandledUnit.AttackMove(unitGroup.Key.Center);
                    continue;
                }

                var path = Pathfinder.FindPath(unitRegion, unitGroup.Key, regionsToAvoid);
                if (path == null) {
                    // Trying to gracefully handle a case that I don't think should happen
                    unhandledUnit.AttackMove(unitGroup.Key.Center);
                    continue;
                }

                var nextRegion = path
                    .Skip(1) // TODO GD Does the path include the starting region?
                    .First();

                unhandledUnit.AttackMove(nextRegion.Center);
            }
        }
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
