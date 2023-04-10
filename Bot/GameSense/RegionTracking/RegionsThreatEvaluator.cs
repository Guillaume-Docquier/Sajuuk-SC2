using System.Collections.Generic;
using System.Linq;
using Bot.Algorithms;
using Bot.ExtensionMethods;
using Bot.MapKnowledge;

namespace Bot.GameSense.RegionTracking;

public class RegionsThreatEvaluator : RegionsEvaluator {
    private readonly IRegionsEvaluator _forceEvaluator;
    private readonly IRegionsEvaluator _opponentValueEvaluator;

    public RegionsThreatEvaluator(RegionsForceEvaluator forceEvaluator, RegionsValueEvaluator opponentValueEvaluator)
        : base("threat", new List<IRegionsEvaluator> { forceEvaluator, opponentValueEvaluator }) {
        _forceEvaluator = forceEvaluator;
        _opponentValueEvaluator = opponentValueEvaluator;
    }

    /// <summary>
    /// Evaluate the threat level of each region.
    /// The threat is defined as Force * Sum(Value / Distance)
    /// Where the sum is over all reachable regions and the value is the value of the opponent.
    ///
    /// In other words, a high threat is achieved with a strong force close to valuable regions.
    /// </summary>
    /// <param name="regions"></param>
    /// <returns></returns>
    protected override IEnumerable<(IRegion region, float evaluation)> DoUpdateEvaluations(IReadOnlyCollection<IRegion> regions) {
        var reach = ComputeRegionsValueReach(regions);

        foreach (var region in regions) {
            var force = _forceEvaluator.GetEvaluation(region, normalized: true);
            if (force == 0) {
                yield return (region, 0);
            }

            // TODO GD Change pathfinder signature to allow specifying reachable regions instead of blocked regions
            var unreachableRegions = regions.Except(reach[region]).ToHashSet();
            var threatenedValueModifier = reach[region]
                .Where(reachableRegion => _opponentValueEvaluator.GetEvaluation(reachableRegion) > 0)
                // Pathfinding far regions first will leverage the pathfinding cache
                .OrderByDescending(valuableReachableRegion => valuableReachableRegion.Center.DistanceTo(region.Center))
                .Sum(valuableReachableRegion => {
                    var value = _opponentValueEvaluator.GetEvaluation(valuableReachableRegion, normalized: true);
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
    private Dictionary<IRegion, List<IRegion>> ComputeRegionsValueReach(IEnumerable<IRegion> regions) {
        var reach = new Dictionary<IRegion, List<IRegion>>();
        foreach (var startingRegion in regions) {
            // TODO GD We can greatly optimize this by using dynamic programming
            reach[startingRegion] = TreeSearch.BreadthFirstSearch(
                startingRegion,
                region => region.GetReachableNeighbors(),
                region => _opponentValueEvaluator.GetEvaluation(region) > 0
            ).ToList();
        }

        return reach;
    }
}
