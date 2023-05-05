using Bot.GameSense;
using Bot.Managers.EconomyManagement.TownHallSupervision;
using SC2APIProtocol;

namespace Bot.Managers.EconomyManagement;

public class EconomySupervisorFactory : IEconomySupervisorFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IBuildingTracker _buildingTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly ICreepTracker _creepTracker;

    public EconomySupervisorFactory(
        IUnitsTracker unitsTracker,
        IBuildingTracker buildingTracker,
        IRegionsTracker regionsTracker,
        ICreepTracker creepTracker
    ) {
        _unitsTracker = unitsTracker;
        _buildingTracker = buildingTracker;
        _regionsTracker = regionsTracker;
        _creepTracker = creepTracker;
    }

    public TownHallSupervisor CreateTownHallSupervisor(Unit townHall, Color color) {
        return new TownHallSupervisor(_unitsTracker, _buildingTracker, _regionsTracker, _creepTracker, townHall, color);
    }
}
