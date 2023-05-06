using System.Collections.Generic;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameSense;
using Bot.MapAnalysis.RegionAnalysis;

namespace Bot.Managers.ScoutManagement.ScoutingTasks;

public class ScoutingTaskFactory : IScoutingTaskFactory {
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;

    public ScoutingTaskFactory(
        IVisibilityTracker visibilityTracker,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IGraphicalDebugger graphicalDebugger
    ) {
        _visibilityTracker = visibilityTracker;
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _graphicalDebugger = graphicalDebugger;
    }

    public ExpandScoutingTask CreateExpandScoutingTask(Vector2 scoutLocation, int priority, int maxScouts, bool waitForExpand = false) {
        return new ExpandScoutingTask(_visibilityTracker, _unitsTracker, _terrainTracker, scoutLocation, priority, maxScouts, waitForExpand);
    }

    public MaintainVisibilityScoutingTask CreateMaintainVisibilityScoutingTask(IReadOnlyCollection<Vector2> area, int priority, int maxScouts) {
        return new MaintainVisibilityScoutingTask(_visibilityTracker, _terrainTracker, _graphicalDebugger, area, priority, maxScouts);
    }

    public RegionScoutingTask CreateRegionScoutingTask(IRegion region, int priority = 0, int maxScouts = 999) {
        return new RegionScoutingTask(_visibilityTracker, region, priority, maxScouts);
    }
}
