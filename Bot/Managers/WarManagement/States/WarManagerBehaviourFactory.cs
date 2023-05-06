using Bot.Builds;
using Bot.Debugging;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameSense;
using Bot.GameSense.RegionsEvaluationsTracking;
using Bot.Managers.ScoutManagement;
using Bot.Managers.ScoutManagement.ScoutingTasks;
using Bot.Managers.WarManagement.States.EarlyGame;
using Bot.Managers.WarManagement.States.Finisher;
using Bot.Managers.WarManagement.States.MidGame;
using Bot.Tagging;

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
        IScoutingTaskFactory scoutingTaskFactory
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
    }

    public EarlyGameBehaviour CreateEarlyGameBehaviour(WarManager warManager) {
        return new EarlyGameBehaviour(warManager, _taggingService, _debuggingFlagsTracker, _unitsTracker, _regionsTracker, _regionsEvaluationsTracker, _warSupervisorFactory, _buildRequestFactory, _graphicalDebugger);
    }

    public MidGameBehaviour CreateMidGameBehaviour(WarManager warManager) {
        return new MidGameBehaviour(warManager, _visibilityTracker, _debuggingFlagsTracker, _unitsTracker, _regionsTracker, _regionsEvaluationsTracker, _scoutSupervisorFactory, _warSupervisorFactory, _buildRequestFactory, _graphicalDebugger, _scoutingTaskFactory);
    }

    public FinisherBehaviour CreateFinisherBehaviour(WarManager warManager) {
        return new FinisherBehaviour(warManager, _taggingService, _enemyRaceTracker, _visibilityTracker, _debuggingFlagsTracker, _unitsTracker, _terrainTracker, _regionsTracker, _warSupervisorFactory, _buildRequestFactory, _graphicalDebugger);
    }
}
