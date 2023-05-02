using System;
using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.MapAnalysis;
using Bot.MapAnalysis.RegionAnalysis;

namespace Bot.GameSense.RegionsEvaluationsTracking;

public class RegionsThreatEvaluator : RegionsEvaluator {
    private readonly IRegionsEvaluator _forceEvaluator;
    private readonly IRegionsEvaluator _opponentValueEvaluator;

    public RegionsThreatEvaluator(RegionsForceEvaluator forceEvaluator, RegionsValueEvaluator opponentValueEvaluator, Func<uint> getCurrentFrame)
        : base("threat", getCurrentFrame, new List<IRegionsEvaluator> { forceEvaluator, opponentValueEvaluator }) {
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
        var valuableRegions = regions.Where(region => _opponentValueEvaluator.GetEvaluation(region) > 0).ToList();

        foreach (var region in regions) {
            var normalizedForce = _forceEvaluator.GetEvaluation(region, normalized: true);
            if (normalizedForce == 0) {
                yield return (region, 0);
            }

            var threatenedValueModifier = valuableRegions
                // Pathfinding far regions first will leverage the pathfinding cache
                .OrderByDescending(valuableRegion => valuableRegion.Center.DistanceTo(region.Center))
                .Sum(valuableRegion => {
                    var normalizedValue = _opponentValueEvaluator.GetEvaluation(valuableRegion, normalized: true);
                    var path = Pathfinder.Instance.FindPath(region, valuableRegion);
                    if (path == null) {
                        return 0;
                    }

                    // TODO GD Maybe we need to reduce the distance to give more value to far value
                    var distance = path.GetPathDistance();

                    // ]0, 1]
                    return normalizedValue / (distance + 1f);
                });

            yield return (region, normalizedForce * threatenedValueModifier);
        }
    }
}
