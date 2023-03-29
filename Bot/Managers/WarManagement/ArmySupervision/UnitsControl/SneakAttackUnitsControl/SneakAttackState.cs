using System.Collections.Generic;
using Bot.StateManagement;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;

public abstract class SneakAttackState: State<SneakAttack> {
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
