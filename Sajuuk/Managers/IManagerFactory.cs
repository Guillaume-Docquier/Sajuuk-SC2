using Sajuuk.Builds;
using Sajuuk.Managers.EconomyManagement;
using Sajuuk.Managers.ScoutManagement;
using Sajuuk.Managers.WarManagement;

namespace Sajuuk.Managers;

public interface IManagerFactory {
    public BuildManager CreateBuildManager(IBuildOrder buildOrder);
    public SupplyManager CreateSupplyManager(BuildManager buildManager);
    public ScoutManager CreateScoutManager();
    public EconomyManager CreateEconomyManager(BuildManager buildManager);
    public WarManager CreateWarManager();
    public CreepManager CreateCreepManager();
    public UpgradesManager CreateUpgradesManager();
}
