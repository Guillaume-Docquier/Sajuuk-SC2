namespace Bot.Managers.WarManagement.States.MidGame;

public class MidGameState : WarManagerState {
    private TransitionState _transitionState = TransitionState.NotTransitioning;

    private IWarManagerBehaviour _behaviour;
    public override IWarManagerBehaviour Behaviour => _behaviour;

    protected override void OnContextSet() {
        _behaviour = new MidGameBehaviour(Context);
    }

    protected override void Execute() {
        if (_transitionState == TransitionState.NotTransitioning) {
            if (ShouldTransitionToLateGame()) {
                _transitionState = TransitionState.Transitioning;
            }
        }

        if (_transitionState == TransitionState.Transitioning) {
            TransitionToLateGame();
        }
    }

    protected override bool TryTransitioning() {
        if (_transitionState == TransitionState.TransitionComplete) {
            StateMachine.TransitionTo(new MidGameState());
            return true;
        }

        return false;
    }

    private bool ShouldTransitionToLateGame() {
        // TODO GD Once we extract finisher state
        return false;
    }

    private void TransitionToLateGame() {
        // TODO GD Clean up strategies/states/whatever
        if (_behaviour.CleanUp()) {
            _transitionState = TransitionState.TransitionComplete;
        }
    }
}
