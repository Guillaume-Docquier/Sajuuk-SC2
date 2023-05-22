using Bot.Builds;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.EnemyStrategyTracking;
using Bot.Managers.EconomyManagement;
using Bot.Managers.ScoutManagement;
using Bot.Managers.ScoutManagement.ScoutingStrategies;
using Bot.Managers.WarManagement;
using Bot.Managers.WarManagement.States;
using Bot.MapAnalysis;
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
    private readonly IEconomySupervisorFactory _economySupervisorFactory;
    private readonly IScoutSupervisorFactory _scoutSupervisorFactory;
    private readonly IWarManagerStateFactory _warManagerStateFactory;
    private readonly IBuildRequestFactory _buildRequestFactory;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IScoutingStrategyFactory _scoutingStrategyFactory;
    private readonly IController _controller;
    private readonly IFrameClock _frameClock;
    private readonly KnowledgeBase _knowledgeBase;
    private readonly ISpendingTracker _spendingTracker;
    private readonly IPathfinder _pathfinder;

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
        IEconomySupervisorFactory economySupervisorFactory,
        IScoutSupervisorFactory scoutSupervisorFactory,
        IWarManagerStateFactory warManagerStateFactory,
        IBuildRequestFactory buildRequestFactory,
        IGraphicalDebugger graphicalDebugger,
        IScoutingStrategyFactory scoutingStrategyFactory,
        IController controller,
        IFrameClock frameClock,
        KnowledgeBase knowledgeBase,
        ISpendingTracker spendingTracker,
        IPathfinder pathfinder
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
        _economySupervisorFactory = economySupervisorFactory;
        _scoutSupervisorFactory = scoutSupervisorFactory;
        _warManagerStateFactory = warManagerStateFactory;
        _buildRequestFactory = buildRequestFactory;
        _graphicalDebugger = graphicalDebugger;
        _scoutingStrategyFactory = scoutingStrategyFactory;
        _controller = controller;
        _frameClock = frameClock;
        _knowledgeBase = knowledgeBase;
        _spendingTracker = spendingTracker;
        _pathfinder = pathfinder;
    }

    public BuildManager CreateBuildManager(IBuildOrder buildOrder) {
        return new BuildManager(_taggingService, _enemyStrategyTracker, _controller, buildOrder);
    }

    public SupplyManager CreateSupplyManager(BuildManager buildManager) {
        return new SupplyManager(_unitsTracker, _buildRequestFactory, _controller, buildManager);
    }

    public ScoutManager CreateScoutManager() {
        return new ScoutManager(_enemyRaceTracker, _unitsTracker, _terrainTracker, _scoutSupervisorFactory, _scoutingStrategyFactory);
    }

    public EconomyManager CreateEconomyManager(BuildManager buildManager) {
        return new EconomyManager(_unitsTracker, _terrainTracker, _buildingTracker, _regionsTracker, _creepTracker, _economySupervisorFactory, _buildRequestFactory, _graphicalDebugger, _controller, _frameClock, _knowledgeBase, _spendingTracker, _pathfinder, buildManager);
    }

    public WarManager CreateWarManager() {
        return new WarManager(_warManagerStateFactory, _graphicalDebugger);
    }

    public CreepManager CreateCreepManager() {
        return new CreepManager(_visibilityTracker, _unitsTracker, _terrainTracker, _buildingTracker, _regionsTracker, _creepTracker, _graphicalDebugger, _frameClock);
    }

    public UpgradesManager CreateUpgradesManager() {
        return new UpgradesManager(_unitsTracker, _buildRequestFactory, _controller, _knowledgeBase);
    }
}
