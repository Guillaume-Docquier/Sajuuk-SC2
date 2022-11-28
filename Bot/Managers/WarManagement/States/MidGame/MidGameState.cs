using Bot.StateManagement;

namespace Bot.Managers.WarManagement.States.MidGame;

public class MidGameState : State<WarManagerBehaviour> {
    private TransitionState _transitionState = TransitionState.NotTransitioning;

    protected override void OnContextSet() {
        Context.RecruitmentPhaseStrategy = new MidGameRecruitmentPhaseStrategy(Context.WarManager);
        Context.DispatchPhaseStrategy = new MidGameDispatchPhaseStrategy(Context.WarManager);
        Context.ManagementPhaseStrategy = new MidGameManagementPhaseStrategy(Context.WarManager);

        Context.Assigner = new WarManager.WarManagerAssigner(Context.WarManager);
        Context.Dispatcher = new WarManager.WarManagerDispatcher(Context.WarManager);
        Context.Releaser = new WarManager.WarManagerReleaser(Context.WarManager);
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
        _transitionState = TransitionState.TransitionComplete;
    }
}
