using Bot.StateManagement;

namespace Bot.Managers.WarManagement.States.MidGame;

public class State : State<WarManager> {
    private TransitionState _transitionState = TransitionState.NotTransitioning;

    protected override void OnContextSet() {
        Context.RecruitmentPhaseStrategy = new RecruitmentPhaseStrategy(Context);
        Context.DispatchPhaseStrategy = new DispatchPhaseStrategy(Context);
        Context.ManagementPhaseStrategy = new ManagementPhaseStrategy(Context);
        Context.SetAssigner(new WarManager.WarManagerAssigner(Context));
        Context.SetDispatcher(new WarManager.WarManagerDispatcher(Context));
        Context.SetReleaser(new WarManager.WarManagerReleaser(Context));
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
            StateMachine.TransitionTo(new MidGame.State());
            return true;
        }

        return false;
    }

    private bool ShouldTransitionToLateGame() {
        // TODO GD Do that if needed
        return false;
    }

    private void TransitionToLateGame() {
        // TODO GD Clean up strategies/states/whatever
        _transitionState = TransitionState.TransitionComplete;
    }
}
