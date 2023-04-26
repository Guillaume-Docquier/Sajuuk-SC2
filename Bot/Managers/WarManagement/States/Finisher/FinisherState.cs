using Bot.Debugging;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
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
    private readonly IExpandAnalyzer _expandAnalyzer;
    private readonly IRegionAnalyzer _regionAnalyzer;
    private readonly IRegionTracker _regionTracker;

    private IWarManagerBehaviour _behaviour;

    public override IWarManagerBehaviour Behaviour => _behaviour;

    public FinisherState(
        ITaggingService taggingService,
        IEnemyRaceTracker enemyRaceTracker,
        IVisibilityTracker visibilityTracker,
        IDebuggingFlagsTracker debuggingFlagsTracker,
        IUnitsTracker unitsTracker,
        IMapAnalyzer mapAnalyzer,
        IExpandAnalyzer expandAnalyzer,
        IRegionAnalyzer regionAnalyzer,
        IRegionTracker regionTracker
    ) {
        _taggingService = taggingService;
        _enemyRaceTracker = enemyRaceTracker;
        _visibilityTracker = visibilityTracker;
        _debuggingFlagsTracker = debuggingFlagsTracker;
        _unitsTracker = unitsTracker;
        _mapAnalyzer = mapAnalyzer;
        _expandAnalyzer = expandAnalyzer;
        _regionAnalyzer = regionAnalyzer;
        _regionTracker = regionTracker;
    }

    protected override void OnContextSet() {
        _behaviour = new FinisherBehaviour(
            Context,
            _taggingService,
            _enemyRaceTracker,
            _visibilityTracker,
            _debuggingFlagsTracker,
            _unitsTracker,
            _mapAnalyzer,
            _expandAnalyzer,
            _regionAnalyzer,
            _regionTracker
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
