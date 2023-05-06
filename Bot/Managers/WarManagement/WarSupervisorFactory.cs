using Bot.Debugging.GraphicalDebugging;
using Bot.GameSense;
using Bot.GameSense.RegionsEvaluationsTracking;
using Bot.Managers.WarManagement.ArmySupervision;
using Bot.Managers.WarManagement.ArmySupervision.RegionalArmySupervision;
using Bot.MapAnalysis.RegionAnalysis;

namespace Bot.Managers.WarManagement;

public class WarSupervisorFactory : IWarSupervisorFactory {
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IRegionsEvaluationsTracker _regionsEvaluationsTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;

    public WarSupervisorFactory(
        IVisibilityTracker visibilityTracker,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        IRegionsEvaluationsTracker regionsEvaluationsTracker,
        IGraphicalDebugger graphicalDebugger
    ) {
        _visibilityTracker = visibilityTracker;
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _regionsTracker = regionsTracker;
        _regionsEvaluationsTracker = regionsEvaluationsTracker;
        _graphicalDebugger = graphicalDebugger;
    }

    public ArmySupervisor CreateArmySupervisor() {
        return new ArmySupervisor(_visibilityTracker, _unitsTracker, _terrainTracker, _regionsTracker, _regionsEvaluationsTracker, _graphicalDebugger);
    }

    public RegionalArmySupervisor CreateRegionalArmySupervisor(IRegion region) {
        return new RegionalArmySupervisor(_unitsTracker, _terrainTracker, _regionsTracker, _regionsEvaluationsTracker, _graphicalDebugger, region);
    }
}
