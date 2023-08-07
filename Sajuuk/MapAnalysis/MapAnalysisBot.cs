using System.Collections.Generic;
using System.Threading.Tasks;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis.RegionAnalysis;
using Sajuuk.Wrapper;
using SC2APIProtocol;

namespace Sajuuk.MapAnalysis;

public class MapAnalysisBot : IBot {
    private readonly IFrameClock _frameClock;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IRegionAnalyzer _regionAnalyzer;

    public Race Race => Race.Zerg;

    public MapAnalysisBot(
        IFrameClock frameClock,
        ITerrainTracker terrainTracker,
        IGraphicalDebugger graphicalDebugger,
        IRegionAnalyzer regionAnalyzer
    ) {
        _frameClock = frameClock;
        _terrainTracker = terrainTracker;
        _graphicalDebugger = graphicalDebugger;
        _regionAnalyzer = regionAnalyzer;
    }

    public Task OnFrame() {
        if (_frameClock.CurrentFrame == 0) {
            Logger.Important("Starting map analysis. Expect the game to freeze for a while.");
        }

        if (!_regionAnalyzer.IsAnalysisComplete) {
            // DebugCoordinates();
        }

        return Task.CompletedTask;
    }

    private void DebugCoordinates() {
        foreach (var cell in _terrainTracker.PlayableCells) {
            var textColor = _terrainTracker.IsBuildable(cell, considerObstaclesObstructions: false) ? Colors.Yellow : Colors.Orange;
            var coordsTextGroup = new List<string>
            {
                $"{cell.X,5:F1}",
                $"{cell.Y,5:F1}",
                $"{_terrainTracker.WithWorldHeight(cell).Z,5:F1}",
            };
            _graphicalDebugger.AddTextGroup(coordsTextGroup, size: 10, color: textColor, worldPos: _terrainTracker.WithWorldHeight(cell).ToPoint(xOffset: -0.35f, yOffset: 0.2f));
        }
    }
}
