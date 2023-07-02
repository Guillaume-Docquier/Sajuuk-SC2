using System.Collections.Generic;
using Sajuuk.StateManagement;

namespace Sajuuk.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;

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
