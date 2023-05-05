using Bot.Debugging;
using Bot.GameSense;
using Bot.GameSense.RegionsEvaluationsTracking;
using Bot.Managers.ScoutManagement;
using Bot.Managers.WarManagement.States.EarlyGame;
using Bot.Managers.WarManagement.States.Finisher;
using Bot.Managers.WarManagement.States.MidGame;
using Bot.Tagging;

namespace Bot.Managers.WarManagement.States;

public class WarManagerStateFactory : IWarManagerStateFactory {
    private readonly ITaggingService _taggingService;
    private readonly IEnemyRaceTracker _enemyRaceTracker;
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IRegionsEvaluationsTracker _regionsEvaluationsTracker;
    private readonly IScoutSupervisorFactory _scoutSupervisorFactory;
    private readonly IWarSupervisorFactory _warSupervisorFactory;

    public WarManagerStateFactory(
        ITaggingService taggingService,
        IEnemyRaceTracker enemyRaceTracker,
        IVisibilityTracker visibilityTracker,
        IDebuggingFlagsTracker debuggingFlagsTracker,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        IRegionsEvaluationsTracker regionsEvaluationsTracker,
        IScoutSupervisorFactory scoutSupervisorFactory,
        IWarSupervisorFactory warSupervisorFactory
    ) {
        _taggingService = taggingService;
        _enemyRaceTracker = enemyRaceTracker;
        _visibilityTracker = visibilityTracker;
        _debuggingFlagsTracker = debuggingFlagsTracker;
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _regionsTracker = regionsTracker;
        _regionsEvaluationsTracker = regionsEvaluationsTracker;
        _scoutSupervisorFactory = scoutSupervisorFactory;
        _warSupervisorFactory = warSupervisorFactory;
    }

    public EarlyGameState CreateEarlyGameState() {
        return new EarlyGameState(_taggingService, _debuggingFlagsTracker, _unitsTracker, _regionsTracker, _regionsEvaluationsTracker, _warSupervisorFactory, this);
    }

    public MidGameState CreateMidGameState() {
        return new MidGameState(_visibilityTracker, _debuggingFlagsTracker, _unitsTracker, _terrainTracker, _regionsTracker, _regionsEvaluationsTracker, _scoutSupervisorFactory, _warSupervisorFactory, this);
    }

    public FinisherState CreateFinisherState() {
        return new FinisherState(_taggingService, _enemyRaceTracker, _visibilityTracker, _debuggingFlagsTracker, _unitsTracker, _terrainTracker, _regionsTracker, _warSupervisorFactory);
    }
}
