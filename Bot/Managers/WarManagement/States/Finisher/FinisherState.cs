using Bot.Debugging;
using Bot.GameSense;
using Bot.MapKnowledge;
using Bot.Tagging;

namespace Bot.Managers.WarManagement.States.Finisher;

public class FinisherState : WarManagerState {
    private readonly ITaggingService _taggingService;
    private readonly IEnemyRaceTracker _enemyRaceTracker;
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly IMapAnalyzer _mapAnalyzer;

    private IWarManagerBehaviour _behaviour;

    public override IWarManagerBehaviour Behaviour => _behaviour;

    public FinisherState(
        ITaggingService taggingService,
        IEnemyRaceTracker enemyRaceTracker,
        IVisibilityTracker visibilityTracker,
        IDebuggingFlagsTracker debuggingFlagsTracker,
        IUnitsTracker unitsTracker,
        IMapAnalyzer mapAnalyzer
    ) {
        _taggingService = taggingService;
        _enemyRaceTracker = enemyRaceTracker;
        _visibilityTracker = visibilityTracker;
        _debuggingFlagsTracker = debuggingFlagsTracker;
        _unitsTracker = unitsTracker;
        _mapAnalyzer = mapAnalyzer;
    }

    protected override void OnContextSet() {
        _behaviour = new FinisherBehaviour(Context, _taggingService, _enemyRaceTracker, _visibilityTracker, _debuggingFlagsTracker, _unitsTracker, _mapAnalyzer);
    }

    protected override void Execute() {
        // Nothing, this is a final state
    }

    protected override bool TryTransitioning() {
        // This is a final state
        return false;
    }
}
