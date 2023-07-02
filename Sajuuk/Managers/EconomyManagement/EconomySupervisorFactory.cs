using Sajuuk.Builds;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameSense;
using Sajuuk.Managers.EconomyManagement.TownHallSupervision;
using Sajuuk.UnitModules;
using SC2APIProtocol;

namespace Sajuuk.Managers.EconomyManagement;

public class EconomySupervisorFactory : IEconomySupervisorFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IBuildRequestFactory _buildRequestFactory;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IFrameClock _frameClock;
    private readonly IUnitModuleInstaller _unitModuleInstaller;

    public EconomySupervisorFactory(
        IUnitsTracker unitsTracker,
        IBuildRequestFactory buildRequestFactory,
        IGraphicalDebugger graphicalDebugger,
        IFrameClock frameClock,
        IUnitModuleInstaller unitModuleInstaller
    ) {
        _unitsTracker = unitsTracker;
        _buildRequestFactory = buildRequestFactory;
        _graphicalDebugger = graphicalDebugger;
        _frameClock = frameClock;
        _unitModuleInstaller = unitModuleInstaller;
    }

    public TownHallSupervisor CreateTownHallSupervisor(Unit townHall, Color color) {
        return new TownHallSupervisor(_unitsTracker, _buildRequestFactory, _graphicalDebugger, _frameClock, _unitModuleInstaller, townHall, color);
    }
}
