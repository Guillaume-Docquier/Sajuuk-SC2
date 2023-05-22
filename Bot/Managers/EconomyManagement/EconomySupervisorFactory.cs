using Bot.Builds;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameSense;
using Bot.Managers.EconomyManagement.TownHallSupervision;
using Bot.MapAnalysis;
using SC2APIProtocol;

namespace Bot.Managers.EconomyManagement;

public class EconomySupervisorFactory : IEconomySupervisorFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IBuildingTracker _buildingTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly ICreepTracker _creepTracker;
    private readonly IBuildRequestFactory _buildRequestFactory;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IFrameClock _frameClock;
    private readonly IPathfinder _pathfinder;

    public EconomySupervisorFactory(
        IUnitsTracker unitsTracker,
        IBuildingTracker buildingTracker,
        IRegionsTracker regionsTracker,
        ICreepTracker creepTracker,
        IBuildRequestFactory buildRequestFactory,
        IGraphicalDebugger graphicalDebugger,
        IFrameClock frameClock,
        IPathfinder pathfinder
    ) {
        _unitsTracker = unitsTracker;
        _buildingTracker = buildingTracker;
        _regionsTracker = regionsTracker;
        _creepTracker = creepTracker;
        _buildRequestFactory = buildRequestFactory;
        _graphicalDebugger = graphicalDebugger;
        _frameClock = frameClock;
        _pathfinder = pathfinder;
    }

    public TownHallSupervisor CreateTownHallSupervisor(Unit townHall, Color color) {
        return new TownHallSupervisor(_unitsTracker, _buildingTracker, _regionsTracker, _creepTracker, _buildRequestFactory, _graphicalDebugger, _frameClock, _pathfinder, townHall, color);
    }
}
