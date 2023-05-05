using Bot.Debugging;
using Bot.GameSense;
using Bot.GameSense.RegionsEvaluationsTracking;
using Bot.Tagging;
using Bot.Utils;

namespace Bot.Managers.WarManagement.States.EarlyGame;

public class EarlyGameState : WarManagerState {
    private const int EarlyGameEndInSeconds = (int)(5 * 60);

    private readonly ITaggingService _taggingService;
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IRegionsEvaluationsTracker _regionsEvaluationsTracker;
    private readonly IWarSupervisorFactory _warSupervisorFactory;
    private readonly IWarManagerStateFactory _warManagerStateFactory;

    private TransitionState _transitionState = TransitionState.NotTransitioning;
    private IWarManagerBehaviour _behaviour;

    public EarlyGameState(
        ITaggingService taggingService,
        IDebuggingFlagsTracker debuggingFlagsTracker,
        IUnitsTracker unitsTracker,
        IRegionsTracker regionsTracker,
        IRegionsEvaluationsTracker regionsEvaluationsTracker,
        IWarSupervisorFactory warSupervisorFactory,
        IWarManagerStateFactory warManagerStateFactory
    ) {
        _taggingService = taggingService;
        _debuggingFlagsTracker = debuggingFlagsTracker;
        _unitsTracker = unitsTracker;
        _regionsTracker = regionsTracker;
        _regionsEvaluationsTracker = regionsEvaluationsTracker;
        _warSupervisorFactory = warSupervisorFactory;
        _warManagerStateFactory = warManagerStateFactory;
    }

    public override IWarManagerBehaviour Behaviour => _behaviour;

    protected override void OnContextSet() {
        _behaviour = new EarlyGameBehaviour(Context, _taggingService, _debuggingFlagsTracker, _unitsTracker, _regionsTracker, _regionsEvaluationsTracker, _warSupervisorFactory);
    }

    protected override void Execute() {
        if (_transitionState == TransitionState.NotTransitioning) {
            if (ShouldTransitionToMidGame()) {
                _transitionState = TransitionState.Transitioning;
            }
        }

        if (_transitionState == TransitionState.Transitioning) {
            TransitionToMidGame();
        }
    }

    protected override bool TryTransitioning() {
        if (_transitionState == TransitionState.TransitionComplete) {
            StateMachine.TransitionTo(_warManagerStateFactory.CreateMidGameState());

            return true;
        }

        return false;
    }

    private static bool ShouldTransitionToMidGame() {
        return Controller.Frame > TimeUtils.SecsToFrames(EarlyGameEndInSeconds);
    }

    private void TransitionToMidGame() {
        // TODO GD Wire the clean up sequence / mechanism? Figure that out anyways
        if (_behaviour.CleanUp()) {
            _transitionState = TransitionState.TransitionComplete;
        }
    }
}
