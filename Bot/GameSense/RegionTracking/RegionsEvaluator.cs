using System.Collections.Generic;
using System.Linq;
using Bot.MapKnowledge;

namespace Bot.GameSense.RegionTracking;

public abstract class RegionsEvaluator : IRegionsEvaluator {
    private readonly List<IRegionsEvaluator> _dependencies;
    private readonly string _evaluatedPropertyName;
    private readonly Dictionary<IRegion, float> _evaluations = new Dictionary<IRegion, float>();
    private readonly Dictionary<IRegion, float> _normalizedEvaluations = new Dictionary<IRegion, float>();

    private ulong _lastEvaluation = 0;

    /// <param name="dependencies">Any dependency upon which the evaluator relies on, that need to be updated beforehand.</param>
    /// <param name="evaluatedPropertyName">A string that describes what this evaluator is evaluating used for logging purposes.</param>
    protected RegionsEvaluator(string evaluatedPropertyName, List<IRegionsEvaluator> dependencies = null) {
        // TODO GD We should be able to specify INeedUpdating, like the RegionsTracker
        // TODO GD They should all have the "once per frame update" logic and "update my dependencies before myself" logic
        // TODO GD Although, with a high dependency count, we'll do a lot of redundant if checks, but maybe that's worth it?
        _dependencies = dependencies ?? new List<IRegionsEvaluator>();
        _evaluatedPropertyName = evaluatedPropertyName;
    }

    /// <summary>
    /// Gets the evaluated property of the provided region.
    /// </summary>
    /// <param name="region">The region to get the evaluated property of.</param>
    /// <param name="normalized">Whether or not to get the normalized property between 0 and 1.</param>
    /// <returns>The evaluated property of the region.</returns>
    public float GetEvaluation(IRegion region, bool normalized = false) {
        UpdateEvaluations();

        if (region == null || !_evaluations.ContainsKey(region)) {
            Logger.Error($"Trying to get the {_evaluatedPropertyName} of an unknown region: {region}. {_evaluations.Count} regions are known.");
            return 0;
        }

        if (normalized) {
            return _normalizedEvaluations[region];
        }

        return _evaluations[region];
    }

    public void Init(IEnumerable<IRegion> regions) {
        foreach (var region in regions) {
            _evaluations[region] = 0;
            _normalizedEvaluations[region] = 0;
        }
    }

    public void UpdateEvaluations() {
        if (IsUpToDate()) {
            return;
        }

        _lastEvaluation = Controller.Frame;
        _dependencies.ForEach(dependency => dependency.UpdateEvaluations());

        foreach (var (region, evaluation) in DoUpdateEvaluations(_evaluations.Keys)) {
            _evaluations[region] = evaluation;
        }

        NormalizeEvaluations();
    }

    protected abstract IEnumerable<(IRegion region, float evaluation)> DoUpdateEvaluations(IReadOnlyCollection<IRegion> regions);

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

    private bool IsUpToDate() {
        return _lastEvaluation >= Controller.Frame;
    }
}
