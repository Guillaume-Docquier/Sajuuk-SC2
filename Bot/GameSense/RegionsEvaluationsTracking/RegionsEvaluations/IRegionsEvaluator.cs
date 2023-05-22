using System.Collections.Generic;
using Bot.MapAnalysis.RegionAnalysis;

namespace Bot.GameSense.RegionsEvaluationsTracking.RegionsEvaluations;

public interface IRegionsEvaluator {
    void Init(IEnumerable<IRegion> regions);

    void UpdateEvaluations();

    float GetEvaluation(IRegion region, bool normalized = false);
}
