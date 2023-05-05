using Bot.Managers.EconomyManagement.TownHallSupervision;
using SC2APIProtocol;

namespace Bot.Managers.EconomyManagement;

public interface IEconomySupervisorFactory {
    public TownHallSupervisor CreateTownHallSupervisor(Unit townHall, Color color);
}
