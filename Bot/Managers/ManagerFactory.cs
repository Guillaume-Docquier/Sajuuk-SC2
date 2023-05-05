using Bot.Builds;
using Bot.Debugging;
using Bot.GameSense;
using Bot.GameSense.EnemyStrategyTracking;
using Bot.GameSense.RegionsEvaluationsTracking;
using Bot.Managers.EconomyManagement;
using Bot.Managers.ScoutManagement;
using Bot.Managers.WarManagement;
using Bot.Tagging;

namespace Bot.Managers;

public class ManagerFactory : IManagerFactory {
    private readonly ITaggingService _taggingService;
    private readonly IEnemyStrategyTracker _enemyStrategyTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly IEnemyRaceTracker _enemyRaceTracker;
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IBuildingTracker _buildingTracker;
    private readonly ICreepTracker _creepTracker;
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;
    private readonly IRegionsEvaluationsTracker _regionsEvaluationsTracker;
    private readonly IEconomySupervisorFactory _economySupervisorFactory;

    public ManagerFactory(
        ITaggingService taggingService,
        IEnemyStrategyTracker enemyStrategyTracker,
        IUnitsTracker unitsTracker,
        IEnemyRaceTracker enemyRaceTracker,
        IVisibilityTracker visibilityTracker,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        IBuildingTracker buildingTracker,
        ICreepTracker creepTracker,
        IDebuggingFlagsTracker debuggingFlagsTracker,
        IRegionsEvaluationsTracker regionsEvaluationsTracker,
        IEconomySupervisorFactory economySupervisorFactory
    ) {
        _taggingService = taggingService;
        _enemyStrategyTracker = enemyStrategyTracker;
        _unitsTracker = unitsTracker;
        _enemyRaceTracker = enemyRaceTracker;
        _visibilityTracker = visibilityTracker;
        _terrainTracker = terrainTracker;
        _regionsTracker = regionsTracker;
        _buildingTracker = buildingTracker;
        _creepTracker = creepTracker;
        _debuggingFlagsTracker = debuggingFlagsTracker;
        _regionsEvaluationsTracker = regionsEvaluationsTracker;
        _economySupervisorFactory = economySupervisorFactory;
    }

    public BuildManager CreateBuildManager(IBuildOrder buildOrder) {
        return new BuildManager(buildOrder, _taggingService, _enemyStrategyTracker);
    }

    public SupplyManager CreateSupplyManager(BuildManager buildManager) {
        return new SupplyManager(buildManager, _unitsTracker);
    }

    public ScoutManager CreateScoutManager() {
        return new ScoutManager(_enemyRaceTracker, _visibilityTracker, _unitsTracker, _terrainTracker, _regionsTracker);
    }

    public EconomyManager CreateEconomyManager(BuildManager buildManager) {
        return new EconomyManager(buildManager, _unitsTracker, _terrainTracker, _buildingTracker, _regionsTracker, _creepTracker, _economySupervisorFactory);
    }

    public WarManager CreateWarManager() {
        return new WarManager(_taggingService, _enemyRaceTracker, _visibilityTracker, _debuggingFlagsTracker, _unitsTracker, _terrainTracker, _regionsTracker, _regionsEvaluationsTracker);
    }

    public CreepManager CreateCreepManager() {
        return new CreepManager(_visibilityTracker, _unitsTracker, _terrainTracker, _buildingTracker, _regionsTracker, _creepTracker);
    }

    public UpgradesManager CreateUpgradesManager() {
        return new UpgradesManager(_unitsTracker);
    }
}
