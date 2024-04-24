using SC2Client;

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
        _logger = logger;
        _analyzers = analyzers;
    }

    public bool IsAnalysisComplete { get; private set; } = false;

    public void OnFrame(IGame game) {
        if (game.CurrentFrame == 0) {
            _logger.Important("Starting map analysis. Expect the game to freeze for a while.");
        }

        foreach (var analyzer in _analyzers) {
            analyzer.OnFrame(game);
        }

        _analyzers = _analyzers
            .Where(analyzer => !analyzer.IsAnalysisComplete)
            .ToList();

        if (_analyzers.Count <= 0 && _quitAt == uint.MaxValue) {
            _quitAt = game.CurrentFrame + 10; // Just give a few frames to debug the analysis
        }

        if (_quitAt <= game.CurrentFrame) {
            IsAnalysisComplete = true;
        }
    }
}
