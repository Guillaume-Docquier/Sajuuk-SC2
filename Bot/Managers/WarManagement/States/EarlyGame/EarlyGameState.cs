using System.Collections.Generic;
using System.Linq;
using Bot.StateManagement;
using Bot.Utils;

namespace Bot.Managers.WarManagement.States.EarlyGame;

public class EarlyGameState : WarManagerState {
    private const int EarlyGameEndInSeconds = (int)(5 * 60);

    private TransitionState _transitionState = TransitionState.NotTransitioning;

    private IWarManagerBehaviour _behaviour;
    public override IWarManagerBehaviour Behaviour => _behaviour;

    protected override void OnContextSet() {
        _behaviour = new EarlyGameBehaviour(Context);
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
