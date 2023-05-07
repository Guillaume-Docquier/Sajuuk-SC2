using Bot.Debugging.GraphicalDebugging;
using Bot.GameSense;
using Bot.Managers.WarManagement.ArmySupervision;
using Bot.Managers.WarManagement.ArmySupervision.RegionalArmySupervision;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl;
using Bot.MapAnalysis.RegionAnalysis;

namespace Bot.Managers.WarManagement;

public class WarSupervisorFactory : IWarSupervisorFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IArmySupervisorStateFactory _armySupervisorStateFactory;
    private readonly IUnitsControlFactory _unitsControlFactory;
    private readonly IRegionalArmySupervisorStateFactory _regionalArmySupervisorStateFactory;

    public WarSupervisorFactory(
        IUnitsTracker unitsTracker,
        IGraphicalDebugger graphicalDebugger,
        IArmySupervisorStateFactory armySupervisorStateFactory,
        IUnitsControlFactory unitsControlFactory,
        IRegionalArmySupervisorStateFactory regionalArmySupervisorStateFactory
    ) {
        _unitsTracker = unitsTracker;
        _graphicalDebugger = graphicalDebugger;
        _armySupervisorStateFactory = armySupervisorStateFactory;
        _unitsControlFactory = unitsControlFactory;
        _regionalArmySupervisorStateFactory = regionalArmySupervisorStateFactory;
    }

    public ArmySupervisor CreateArmySupervisor() {
        return new ArmySupervisor(_armySupervisorStateFactory);
    }

    public RegionalArmySupervisor CreateRegionalArmySupervisor(IRegion region) {
        return new RegionalArmySupervisor(_unitsTracker, _graphicalDebugger, _unitsControlFactory, _regionalArmySupervisorStateFactory, region);
    }
}
