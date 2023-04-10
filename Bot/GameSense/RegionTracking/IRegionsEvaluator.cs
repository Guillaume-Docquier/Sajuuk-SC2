using System.Collections.Generic;
using Bot.MapKnowledge;

namespace Bot.GameSense.RegionTracking;

public interface IRegionsEvaluator {
    void Init(IEnumerable<IRegion> regions);

    void UpdateEvaluations();

    float GetEvaluation(IRegion region, bool normalized = false);
}
