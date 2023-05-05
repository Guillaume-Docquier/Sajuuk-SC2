using Bot.Builds;
using Bot.Managers.EconomyManagement;
using Bot.Managers.ScoutManagement;
using Bot.Managers.WarManagement;

namespace Bot.Managers;

public interface IManagerFactory {
    public BuildManager CreateBuildManager(IBuildOrder buildOrder);
    public SupplyManager CreateSupplyManager(BuildManager buildManager);
    public ScoutManager CreateScoutManager();
    public EconomyManager CreateEconomyManager(BuildManager buildManager);
    public WarManager CreateWarManager();
    public CreepManager CreateCreepManager();
    public UpgradesManager CreateUpgradesManager();
}
