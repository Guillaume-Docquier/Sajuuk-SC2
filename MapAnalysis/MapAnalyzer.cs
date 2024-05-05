using SC2Client;
using SC2Client.State;

namespace MapAnalysis;

/// <summary>
/// The MapAnalyzer runs a series of analyzers until they are all done.
/// The analysis results are saved to files that can be loaded later.
/// </summary>
public class MapAnalyzer : IAnalyzer {
    private readonly ILogger _logger;
    private List<IAnalyzer> _analyzers;

    private uint _quitAt = uint.MaxValue;

    public MapAnalyzer(ILogger logger, List<IAnalyzer> analyzers) {
        _logger = logger.CreateNamed("MapAnalyzer");
        _analyzers = analyzers;
    }

    public bool IsAnalysisComplete { get; private set; } = false;

    public void OnFrame(IGameState gameState) {
        if (gameState.CurrentFrame == 0) {
            _logger.Important("Starting map analysis. Expect the game to freeze for a while.");
        }

        foreach (var analyzer in _analyzers) {
            analyzer.OnFrame(gameState);
        }

        _analyzers = _analyzers
            .Where(analyzer => !analyzer.IsAnalysisComplete)
            .ToList();

        if (_analyzers.Count <= 0 && _quitAt == uint.MaxValue) {
            _quitAt = gameState.CurrentFrame + 10; // Just give a few frames to debug the analysis
        }

        if (_quitAt <= gameState.CurrentFrame) {
            IsAnalysisComplete = true;
        }
    }
}
