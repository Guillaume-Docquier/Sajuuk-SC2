using System.Collections.Generic;
using Bot.MapKnowledge;

namespace Bot.GameSense.RegionTracking;

// TODO GD Highlight the dependency to Force and Value evaluators
public class RegionDefenseEvaluator : IRegionsEvaluator {
    private Dictionary<Region, float> _regionDefenseScores;

    /// <summary>
    /// Gets the defense score of the provided region
    /// </summary>
    /// <param name="region">The region to get the evaluated value of</param>
    /// <returns>The evaluated value of the region</returns>
    public float GetEvaluation(Region region) {
        if (!_regionDefenseScores.ContainsKey(region)) {
            Logger.Error("Trying to get the value of an unknown region. {0} regions are known.", _regionDefenseScores.Count);
            return 0;
        }

        return _regionDefenseScores[region];
    }

    /// <summary>
    /// Initializes the evaluator to track the provided regions.
    /// </summary>
    /// <param name="regions">The regions to evaluate in the future</param>
    public void Init(List<Region> regions) {
        _regionDefenseScores = new Dictionary<Region, float>();
        foreach (var region in regions) {
            _regionDefenseScores[region] = 0f;
        }
    }

    /// <summary>
    /// Evaluates the defense score of each region.
    /// A defense score indicates how important defending a region is. The higher, the better.
    /// </summary>
    public void Evaluate() {
        // for each region
        //     compute the enemy reach as if the region was blocked (defended)
        //     compute the distance from this region to all others
        //     give a defense score based on the region's value, our distance and their distance
        //        if the enemy cannot reach but we can, it's very good
        //        if we're closer than the enemy, that's also good
    }
}
