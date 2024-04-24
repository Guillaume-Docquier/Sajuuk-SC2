using SC2Client;

namespace MapAnalysis;

public interface IAnalyzer {
    bool IsAnalysisComplete { get; }

    void OnFrame(IGame game);
}
