using Bot.Builds;
using Bot.Debugging;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.RegionsEvaluationsTracking;
using Bot.Managers.ScoutManagement;
using Bot.Managers.ScoutManagement.ScoutingTasks;
using Bot.Managers.WarManagement.States.EarlyGame;
using Bot.Managers.WarManagement.States.Finisher;
using Bot.Managers.WarManagement.States.MidGame;
using Bot.MapAnalysis;
using Bot.Tagging;
using Bot.UnitModules;

namespace Bot.Managers.WarManagement.States;

public class WarManagerBehaviourFactory : IWarManagerBehaviourFactory {
    private readonly ITaggingService _taggingService;
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IRegionsEvaluationsTracker _regionsEvaluationsTracker;
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IEnemyRaceTracker _enemyRaceTracker;
    private readonly IScoutSupervisorFactory _scoutSupervisorFactory;
    private readonly IWarSupervisorFactory _warSupervisorFactory;
    private readonly IBuildRequestFactory _buildRequestFactory;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IScoutingTaskFactory _scoutingTaskFactory;
    private readonly TechTree _techTree;
    private readonly IController _controller;
    private readonly IFrameClock _frameClock;
    private readonly IUnitEvaluator _unitEvaluator;
    private readonly IPathfinder _pathfinder;
    private readonly IUnitModuleInstaller _unitModuleInstaller;

    public WarManagerBehaviourFactory(
        ITaggingService taggingService,
        IDebuggingFlagsTracker debuggingFlagsTracker,
        IUnitsTracker unitsTracker,
        IRegionsTracker regionsTracker,
        IRegionsEvaluationsTracker regionsEvaluationsTracker,
        IVisibilityTracker visibilityTracker,
        ITerrainTracker terrainTracker,
        IEnemyRaceTracker enemyRaceTracker,
        IScoutSupervisorFactory scoutSupervisorFactory,
        IWarSupervisorFactory warSupervisorFactory,
        IBuildRequestFactory buildRequestFactory,
        IGraphicalDebugger graphicalDebugger,
        IScoutingTaskFactory scoutingTaskFactory,
        TechTree techTree,
        IController controller,
        IFrameClock frameClock,
        IUnitEvaluator unitEvaluator,
        IPathfinder pathfinder,
        IUnitModuleInstaller unitModuleInstaller
    ) {
        _taggingService = taggingService;
        _debuggingFlagsTracker = debuggingFlagsTracker;
        _unitsTracker = unitsTracker;
        _regionsTracker = regionsTracker;
        _regionsEvaluationsTracker = regionsEvaluationsTracker;
        _visibilityTracker = visibilityTracker;
        _terrainTracker = terrainTracker;
        _enemyRaceTracker = enemyRaceTracker;
        _scoutSupervisorFactory = scoutSupervisorFactory;
        _warSupervisorFactory = warSupervisorFactory;
        _buildRequestFactory = buildRequestFactory;
        _graphicalDebugger = graphicalDebugger;
        _scoutingTaskFactory = scoutingTaskFactory;
        _techTree = techTree;
        _controller = controller;
        _frameClock = frameClock;
        _unitEvaluator = unitEvaluator;
        _pathfinder = pathfinder;
        _unitModuleInstaller = unitModuleInstaller;
    }

    public EarlyGameBehaviour CreateEarlyGameBehaviour(WarManager warManager) {
        return new EarlyGameBehaviour(warManager, _taggingService, _debuggingFlagsTracker, _unitsTracker, _regionsTracker, _regionsEvaluationsTracker, _warSupervisorFactory, _buildRequestFactory, _graphicalDebugger, _techTree, _controller, _unitEvaluator, _pathfinder, _unitModuleInstaller);
    }

    public MidGameBehaviour CreateMidGameBehaviour(WarManager warManager) {
        return new MidGameBehaviour(warManager, _visibilityTracker, _debuggingFlagsTracker, _unitsTracker, _regionsTracker, _regionsEvaluationsTracker, _scoutSupervisorFactory, _warSupervisorFactory, _buildRequestFactory, _graphicalDebugger, _scoutingTaskFactory, _techTree, _controller, _unitEvaluator, _pathfinder, _unitModuleInstaller);
    }

    public FinisherBehaviour CreateFinisherBehaviour(WarManager warManager) {
        return new FinisherBehaviour(warManager, _taggingService, _enemyRaceTracker, _visibilityTracker, _debuggingFlagsTracker, _unitsTracker, _terrainTracker, _regionsTracker, _warSupervisorFactory, _buildRequestFactory, _graphicalDebugger, _frameClock, _controller, _unitEvaluator, _unitModuleInstaller);
    }
}
