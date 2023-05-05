using Bot.Debugging;
using Bot.GameSense;
using Bot.Tagging;

namespace Bot.Managers.WarManagement.States.Finisher;

public class FinisherState : WarManagerState {
    private readonly ITaggingService _taggingService;
    private readonly IEnemyRaceTracker _enemyRaceTracker;
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IWarSupervisorFactory _warSupervisorFactory;

    private IWarManagerBehaviour _behaviour;

    public override IWarManagerBehaviour Behaviour => _behaviour;

    public FinisherState(
        ITaggingService taggingService,
        IEnemyRaceTracker enemyRaceTracker,
        IVisibilityTracker visibilityTracker,
        IDebuggingFlagsTracker debuggingFlagsTracker,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        IWarSupervisorFactory warSupervisorFactory
    ) {
        _taggingService = taggingService;
        _enemyRaceTracker = enemyRaceTracker;
        _visibilityTracker = visibilityTracker;
        _debuggingFlagsTracker = debuggingFlagsTracker;
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _regionsTracker = regionsTracker;
        _warSupervisorFactory = warSupervisorFactory;
    }

    protected override void OnContextSet() {
        _behaviour = new FinisherBehaviour(
            Context,
            _taggingService,
            _enemyRaceTracker,
            _visibilityTracker,
            _debuggingFlagsTracker,
            _unitsTracker,
            _terrainTracker,
            _regionsTracker,
            _warSupervisorFactory
        );
    }

    protected override void Execute() {
        // Nothing, this is a final state
    }

    protected override bool TryTransitioning() {
        // This is a final state
        return false;
    }
}
