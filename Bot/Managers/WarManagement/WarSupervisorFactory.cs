using Bot.Debugging.GraphicalDebugging;
using Bot.GameSense;
using Bot.GameSense.RegionsEvaluationsTracking;
using Bot.Managers.WarManagement.ArmySupervision;
using Bot.Managers.WarManagement.ArmySupervision.RegionalArmySupervision;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl;
using Bot.MapAnalysis.RegionAnalysis;

namespace Bot.Managers.WarManagement;

public class WarSupervisorFactory : IWarSupervisorFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IRegionsEvaluationsTracker _regionsEvaluationsTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IArmySupervisorStateFactory _armySupervisorStateFactory;
    private readonly IUnitsControlFactory _unitsControlFactory;

    public WarSupervisorFactory(
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        IRegionsEvaluationsTracker regionsEvaluationsTracker,
        IGraphicalDebugger graphicalDebugger,
        IArmySupervisorStateFactory armySupervisorStateFactory,
        IUnitsControlFactory unitsControlFactory
    ) {
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _regionsTracker = regionsTracker;
        _regionsEvaluationsTracker = regionsEvaluationsTracker;
        _graphicalDebugger = graphicalDebugger;
        _armySupervisorStateFactory = armySupervisorStateFactory;
        _unitsControlFactory = unitsControlFactory;
    }

    public ArmySupervisor CreateArmySupervisor() {
        return new ArmySupervisor(_armySupervisorStateFactory);
    }

    public RegionalArmySupervisor CreateRegionalArmySupervisor(IRegion region) {
        return new RegionalArmySupervisor(_unitsTracker, _regionsTracker, _regionsEvaluationsTracker, _graphicalDebugger, _unitsControlFactory, region);
    }
}
