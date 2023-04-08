using System.Collections.Generic;
using System.Linq;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.GameSense.RegionTracking;

public abstract class RegionsEvaluator : IRegionsEvaluator {
    /// <summary>
    /// A string that describes what this evaluator is evaluating.
    /// This is used for logging purposes.
    /// </summary>
    private readonly string _evaluatedPropertyName;
    private readonly Dictionary<Region, float> _evaluations = new Dictionary<Region, float>();
    private readonly Dictionary<Region, float> _normalizedEvaluations = new Dictionary<Region, float>();

    protected readonly Alliance Alliance;

    protected RegionsEvaluator(Alliance alliance, string evaluatedPropertyName) {
        Alliance = alliance;
        _evaluatedPropertyName = evaluatedPropertyName;
    }

    /// <summary>
    /// Gets the evaluated property of the provided region.
    /// </summary>
    /// <param name="region">The region to get the evaluated property of.</param>
    /// <param name="normalized">Whether or not to get the normalized property between 0 and 1.</param>
    /// <returns>The evaluated property of the region.</returns>
    public float GetEvaluation(Region region, bool normalized = false) {
        if (region == null || !_evaluations.ContainsKey(region)) {
            Logger.Error($"Trying to get the {_evaluatedPropertyName} of an unknown region: {region}. {_evaluations.Count} regions are known.");
            return 0;
        }

        if (normalized) {
            return _normalizedEvaluations[region];
        }

        return _evaluations[region];
    }

    public void Init(IEnumerable<Region> regions) {
        foreach (var region in regions) {
            _evaluations[region] = 0;
            _normalizedEvaluations[region] = 0;
        }
    }

    public void Evaluate() {
        foreach (var (region, evaluation) in DoEvaluate(_evaluations.Keys)) {
            _evaluations[region] = evaluation;
        }

        NormalizeEvaluations();
    }

    protected abstract IEnumerable<(Region region, float value)> DoEvaluate(IReadOnlyCollection<Region> regions);

    /// <summary>
    /// Normalize all the evaluations to a value between 0 and 1.
    /// The normalized evaluation is its proportion of the total of all evaluations.
    /// i.e a normalized threat of 1 means 100% of all threats.
    /// If all evaluations are 0, normalized evaluations will all be 0.
    /// </summary>
    private void NormalizeEvaluations() {
        var totalValue = _evaluations.Values.Sum();

        foreach (var region in _evaluations.Keys) {
            // We assume all evaluations are positive
            _normalizedEvaluations[region] = totalValue == 0 ? 0 : _evaluations[region] / totalValue;
        }
    }
}
