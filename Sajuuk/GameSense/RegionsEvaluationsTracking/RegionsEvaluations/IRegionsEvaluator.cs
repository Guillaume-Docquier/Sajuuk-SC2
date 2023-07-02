using System.Collections.Generic;
using Sajuuk.MapAnalysis.RegionAnalysis;

namespace Sajuuk.GameSense.RegionsEvaluationsTracking.RegionsEvaluations;

public interface IRegionsEvaluator {
    void Init(IEnumerable<IRegion> regions);

    void UpdateEvaluations();

    float GetEvaluation(IRegion region, bool normalized = false);
}
