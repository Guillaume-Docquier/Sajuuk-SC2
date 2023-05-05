using Bot.Managers.WarManagement.ArmySupervision;
using Bot.Managers.WarManagement.ArmySupervision.RegionalArmySupervision;
using Bot.MapAnalysis.RegionAnalysis;

namespace Bot.Managers.WarManagement;

public interface IWarSupervisorFactory {
    public ArmySupervisor CreateArmySupervisor();
    public RegionalArmySupervisor CreateRegionalArmySupervisor(IRegion region);
}
