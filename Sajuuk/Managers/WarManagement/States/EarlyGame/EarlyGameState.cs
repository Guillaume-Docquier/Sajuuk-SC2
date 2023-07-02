using Sajuuk.Utils;

namespace Sajuuk.Managers.WarManagement.States.EarlyGame;

public class EarlyGameState : WarManagerState {
    private const int EarlyGameEndInSeconds = (int)(5 * 60);

    private readonly IWarManagerStateFactory _warManagerStateFactory;
    private readonly IWarManagerBehaviourFactory _warManagerBehaviourFactory;
    private readonly IFrameClock _frameClock;

    private TransitionState _transitionState = TransitionState.NotTransitioning;
    private IWarManagerBehaviour _behaviour;

    public EarlyGameState(
        IWarManagerStateFactory warManagerStateFactory,
        IWarManagerBehaviourFactory warManagerBehaviourFactory,
        IFrameClock frameClock
    ) {
        _warManagerStateFactory = warManagerStateFactory;
        _warManagerBehaviourFactory = warManagerBehaviourFactory;
        _frameClock = frameClock;
    }

    public override IWarManagerBehaviour Behaviour => _behaviour;

    protected override void OnContextSet() {
        _behaviour = _warManagerBehaviourFactory.CreateEarlyGameBehaviour(Context);
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

    private bool ShouldTransitionToMidGame() {
        return _frameClock.CurrentFrame > TimeUtils.SecsToFrames(EarlyGameEndInSeconds);
    }

    private void TransitionToMidGame() {
        // TODO GD Wire the clean up sequence / mechanism? Figure that out anyways
        if (_behaviour.CleanUp()) {
            _transitionState = TransitionState.TransitionComplete;
        }
    }
}
