using System.Collections.Generic;
using Bot.StateManagement;

namespace Bot.Managers.ArmySupervision.Tactics.SneakAttack;

public abstract class SneakAttackState: State<SneakAttackTactic> {
    protected SneakAttackState NextState = null;

    public abstract bool IsViable(IReadOnlyCollection<Unit> army);

    protected override bool TryTransitioning() {
        if (NextState != null) {
            StateMachine.TransitionTo(NextState);
            return true;
        }

        return false;
    }
}
