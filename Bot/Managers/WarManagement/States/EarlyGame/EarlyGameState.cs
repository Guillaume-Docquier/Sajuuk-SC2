using System.Collections.Generic;
using System.Linq;
using Bot.StateManagement;
using Bot.Utils;

namespace Bot.Managers.WarManagement.States.EarlyGame;

public class EarlyGameState : State<WarManagerBehaviour> {
    private const int EarlyGameEndInSeconds = (int)(5 * 60);

    private readonly List<WarManagerStrategy> _strategies = new List<WarManagerStrategy>();
    private TransitionState _transitionState = TransitionState.NotTransitioning;

    protected override void OnContextSet() {
        Context.RecruitmentPhaseStrategy = new EarlyGameRecruitmentPhaseStrategy(Context.WarManager);
        Context.DispatchPhaseStrategy = new EarlyGameDispatchPhaseStrategy(Context.WarManager);
        Context.ManagementPhaseStrategy = new EarlyGameManagementPhaseStrategy(Context.WarManager);

        Context.Assigner = new WarManager.WarManagerAssigner(Context.WarManager);
        Context.Dispatcher = new WarManager.WarManagerDispatcher(Context.WarManager);
        Context.Releaser = new WarManager.WarManagerReleaser(Context.WarManager);

        _strategies.Add(Context.RecruitmentPhaseStrategy);
        _strategies.Add(Context.DispatchPhaseStrategy);
        _strategies.Add(Context.ManagementPhaseStrategy);
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
            StateMachine.TransitionTo(new MidGame.MidGameState());
            return true;
        }

        return false;
    }

    private bool ShouldTransitionToMidGame() {
        if (Controller.Frame < TimeUtils.SecsToFrames(EarlyGameEndInSeconds)) {
            return false;
        }

        if (!_strategies.All(strategy => strategy.CanTransition())) {
            return false;
        }

        return true;
    }

    private void TransitionToMidGame() {
        // TODO GD Clean up strategies/states/whatever
        _transitionState = TransitionState.TransitionComplete;
    }
}
