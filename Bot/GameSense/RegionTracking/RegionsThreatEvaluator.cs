using System.Collections.Generic;
using System.Linq;
using Bot.Algorithms;
using Bot.ExtensionMethods;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.GameSense.RegionTracking;

public class RegionsThreatEvaluator : RegionsEvaluator {
    public RegionsThreatEvaluator(Alliance alliance) : base(alliance, "threat") {}

    /// <summary>
    /// Evaluate the threat level of each region.
    /// The threat is defined as Force * Sum(Value / Distance)
    /// Where the sum is over all reachable regions and the value is the value of the opponent.
    ///
    /// In other words, a high threat is achieved with a strong force close to valuable regions.
    /// </summary>
    /// <param name="regions"></param>
    /// <returns></returns>
    protected override IEnumerable<(Region region, float value)> DoEvaluate(IReadOnlyCollection<Region> regions) {
        var reach = ComputeRegionsValueReach(regions);
        var opposingAlliance = Alliance.GetOpposing();

        foreach (var region in regions) {
            var force = RegionTracker.GetForce(region, Alliance, normalized: true);
            if (force == 0) {
                yield return (region, 0);
            }

            // TODO GD Change pathfinder signature to allow specifying reachable regions instead of blocked regions
            var unreachableRegions = regions.Except(reach[region]).ToHashSet();
            var threatenedValueModifier = reach[region]
                .Where(reachableRegion => RegionTracker.GetValue(reachableRegion, opposingAlliance) > 0)
                // Pathfinding far regions first will leverage the pathfinding cache
                .OrderByDescending(valuableReachableRegion => valuableReachableRegion.Center.DistanceTo(region.Center))
                .Sum(valuableReachableRegion => {
                    var value = RegionTracker.GetValue(valuableReachableRegion, opposingAlliance, normalized: true);
                    // TODO GD Maybe we need to reduce the distance to give more value to far value
                    var distance = Pathfinder.FindPath(region, valuableReachableRegion, unreachableRegions).GetPathDistance();

                    return value / (distance + 1f);
                });

            yield return (region, force * threatenedValueModifier);
        }
    }
    
    /// <summary>
    /// Computes the reach of each of the provided region.
    /// A region is reachable if there's a path to it that doesn't go through a valuable region.
    /// </summary>
    /// <param name="regions">The regions to compute the reach of.</param>
    /// <returns>Each region and the list of their reachable regions</returns>
    private static Dictionary<Region, List<Region>> ComputeRegionsValueReach(IEnumerable<Region> regions) {
        var reach = new Dictionary<Region, List<Region>>();
        foreach (var startingRegion in regions) {
            // TODO GD We can greatly optimize this by using dynamic programming
            reach[startingRegion] = TreeSearch.BreadthFirstSearch(
                startingRegion,
                region => region.GetReachableNeighbors(),
                region => RegionTracker.GetValue(region, Alliance.Enemy) > 0
            ).ToList();
        }

        return reach;
    }
}
