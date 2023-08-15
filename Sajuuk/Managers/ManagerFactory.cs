using Sajuuk.Builds.BuildOrders;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.GameSense.EnemyStrategyTracking;
using Sajuuk.Managers.EconomyManagement;
using Sajuuk.Managers.ScoutManagement;
using Sajuuk.Managers.ScoutManagement.ScoutingStrategies;
using Sajuuk.Managers.WarManagement;
using Sajuuk.Managers.WarManagement.States;
using Sajuuk.Tagging;
using Sajuuk.UnitModules;

namespace Sajuuk.Managers;

public class ManagerFactory : IManagerFactory {
    private readonly ITaggingService _taggingService;
    private readonly IEnemyStrategyTracker _enemyStrategyTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly IEnemyRaceTracker _enemyRaceTracker;
    private readonly ITerrainTracker _terrainTracker;
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
    private readonly IUnitModuleInstaller _unitModuleInstaller;

    public ManagerFactory(
        ITaggingService taggingService,
        IEnemyStrategyTracker enemyStrategyTracker,
        IUnitsTracker unitsTracker,
        IEnemyRaceTracker enemyRaceTracker,
        ITerrainTracker terrainTracker,
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
        IUnitModuleInstaller unitModuleInstaller
    ) {
        _taggingService = taggingService;
        _enemyStrategyTracker = enemyStrategyTracker;
        _unitsTracker = unitsTracker;
        _enemyRaceTracker = enemyRaceTracker;
        _terrainTracker = terrainTracker;
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
        _unitModuleInstaller = unitModuleInstaller;
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
        return new EconomyManager(_unitsTracker, _terrainTracker, _economySupervisorFactory, _buildRequestFactory, _graphicalDebugger, _controller, _frameClock, _knowledgeBase, _spendingTracker, buildManager, _unitModuleInstaller);
    }

    public WarManager CreateWarManager() {
        return new WarManager(_warManagerStateFactory, _graphicalDebugger);
    }

    public CreepManager CreateCreepManager() {
        return new CreepManager(_unitsTracker, _unitModuleInstaller);
    }

    public UpgradesManager CreateUpgradesManager() {
        return new UpgradesManager(_unitsTracker, _buildRequestFactory, _controller, _knowledgeBase);
    }
}
