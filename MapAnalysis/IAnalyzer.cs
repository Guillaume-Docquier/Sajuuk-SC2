using SC2Client.State;

namespace MapAnalysis;

public interface IAnalyzer {
    bool IsAnalysisComplete { get; }

    void OnFrame(IGameState gameState);
}
