using Sajuuk.Managers.EconomyManagement.TownHallSupervision;
using SC2APIProtocol;

namespace Sajuuk.Managers.EconomyManagement;

public interface IEconomySupervisorFactory {
    public TownHallSupervisor CreateTownHallSupervisor(Unit townHall, Color color);
}
