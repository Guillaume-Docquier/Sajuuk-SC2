using System.Collections.Generic;
using System.Threading.Tasks;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis.ExpandAnalysis;
using Sajuuk.MapAnalysis.RegionAnalysis;
using Sajuuk.Wrapper;
using SC2APIProtocol;

namespace Sajuuk.MapAnalysis;

public class MapAnalysisBot : IBot {
    private readonly IFrameClock _frameClock;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IExpandAnalyzer _expandAnalyzer;
    private readonly IRegionAnalyzer _regionAnalyzer;
    private readonly ISc2Client _sc2Client;

    private uint _quitAt = uint.MaxValue;

    public Race Race => Race.Zerg;

    public MapAnalysisBot(
        IFrameClock frameClock,
        ITerrainTracker terrainTracker,
        IGraphicalDebugger graphicalDebugger,
        IExpandAnalyzer expandAnalyzer,
        IRegionAnalyzer regionAnalyzer,
        ISc2Client sc2Client
    ) {
        _frameClock = frameClock;
        _terrainTracker = terrainTracker;
        _graphicalDebugger = graphicalDebugger;
        _expandAnalyzer = expandAnalyzer;
        _regionAnalyzer = regionAnalyzer;
        _sc2Client = sc2Client;
    }

    public async Task OnFrame() {
        if (_frameClock.CurrentFrame == 0) {
            Logger.Important("Starting map analysis. Expect the game to freeze for a while.");
        }

        if (!_regionAnalyzer.IsAnalysisComplete) {
            // DebugCoordinates();
        }

        if (_expandAnalyzer.IsAnalysisComplete && _regionAnalyzer.IsAnalysisComplete && _quitAt == uint.MaxValue) {
            _quitAt = _frameClock.CurrentFrame + 10; // Just give a few frames to debug the analysis
        }

        if (_quitAt <= _frameClock.CurrentFrame) {
            await _sc2Client.LeaveCurrentGame();
        }
    }

    private void DebugCoordinates() {
        foreach (var cell in _terrainTracker.WalkableCells) {
            var coords = new List<string>
            {
                $"{cell.X,5:F1}",
                $"{cell.Y,5:F1}",
                $"{_terrainTracker.WithWorldHeight(cell).Z,5:F1}",
            };
            _graphicalDebugger.AddTextGroup(coords, size: 10, worldPos: _terrainTracker.WithWorldHeight(cell).ToPoint(xOffset: -0.35f, yOffset: 0.2f));
        }
    }
}
