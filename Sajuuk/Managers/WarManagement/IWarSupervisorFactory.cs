using Sajuuk.Managers.WarManagement.ArmySupervision;
using Sajuuk.Managers.WarManagement.ArmySupervision.RegionalArmySupervision;
using Sajuuk.MapAnalysis.RegionAnalysis;

namespace Sajuuk.Managers.WarManagement;

public interface IWarSupervisorFactory {
    public ArmySupervisor CreateArmySupervisor();
    public RegionalArmySupervisor CreateRegionalArmySupervisor(IRegion region);
}
