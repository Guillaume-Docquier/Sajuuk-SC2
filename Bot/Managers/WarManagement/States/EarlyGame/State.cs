using Bot.StateManagement;

namespace Bot.Managers.WarManagement.States.EarlyGame;

public class State : State<WarManagerBehaviour> {
    private TransitionState _transitionState = TransitionState.NotTransitioning;

    protected override void OnContextSet() {
        Context.RecruitmentPhaseStrategy = new RecruitmentPhaseStrategy(Context.WarManager);
        Context.DispatchPhaseStrategy = new DispatchPhaseStrategy(Context.WarManager);
        Context.ManagementPhaseStrategy = new ManagementPhaseStrategy(Context.WarManager);
        Context.Assigner = new WarManager.WarManagerAssigner(Context.WarManager);
        Context.Dispatcher = new WarManager.WarManagerDispatcher(Context.WarManager);
        Context.Releaser = new WarManager.WarManagerReleaser(Context.WarManager);
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
            StateMachine.TransitionTo(new MidGame.State());
            return true;
        }

        return false;
    }

    private bool ShouldTransitionToMidGame() {
        // TODO GD Timing + current rush state
        return true;
    }

    private void TransitionToMidGame() {
        // TODO GD Clean up strategies/states/whatever
        _transitionState = TransitionState.TransitionComplete;
    }
}
