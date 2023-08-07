using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameData;
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
    private readonly IUnitsTracker _unitsTracker;
    private readonly IRequestBuilder _requestBuilder;
    private readonly ISc2Client _sc2Client;

    public Race Race => Race.Zerg;

    public MapAnalysisBot(
        IFrameClock frameClock,
        ITerrainTracker terrainTracker,
        IGraphicalDebugger graphicalDebugger,
        IRegionAnalyzer regionAnalyzer,
        IUnitsTracker unitsTracker,
        IRequestBuilder requestBuilder,
        ISc2Client sc2Client
    ) {
        _frameClock = frameClock;
        _terrainTracker = terrainTracker;
        _graphicalDebugger = graphicalDebugger;
        _regionAnalyzer = regionAnalyzer;
        _unitsTracker = unitsTracker;
        _requestBuilder = requestBuilder;
        _sc2Client = sc2Client;
    }

    public async Task OnFrame() {
        if (_frameClock.CurrentFrame == 0) {
            Logger.Important("Starting map analysis. Expect the game to freeze for a while.");

            await _sc2Client.SendRequest(_requestBuilder.DebugRevealMap());
        }

        // We need to see the rocks to kill them otherwise we use the snapshot id
        // Revealing takes a few frames, which is why we're spamming like this
        // We kill obstacles during map analysis because their footprint is inexact and prevent us from truly identifying walkable cells.
        var obstaclesUnitTags = _unitsTracker.GetUnits(_unitsTracker.NeutralUnits, Units.Obstacles).Select(unit => unit.Tag);
        await _sc2Client.SendRequest(_requestBuilder.DebugKillUnits(obstaclesUnitTags));

        // TODO GD Run Analyzers from here instead of from the controller
        // TODO GD Save and load the playable tiles? --> Technically the region data contains all playable cells!
        // TODO GD Killing rocks might be problematic for rocks that block expands?
        // TODO GD We can have a different terrain tracker for map analysis that can snapshot obstacles before we kill them
        if (!_regionAnalyzer.IsAnalysisComplete) {
            DebugCoordinates();
        }
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
