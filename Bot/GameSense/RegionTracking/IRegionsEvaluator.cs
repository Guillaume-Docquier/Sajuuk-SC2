using System.Collections.Generic;
using Bot.MapKnowledge;

namespace Bot.GameSense.RegionTracking;

public interface IRegionsEvaluator {
    void Init(IEnumerable<Region> regions);

    void Evaluate();

    float GetEvaluation(Region region, bool normalized = false);
}
