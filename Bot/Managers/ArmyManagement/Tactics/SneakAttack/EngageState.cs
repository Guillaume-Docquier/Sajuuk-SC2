using System.Collections.Generic;

namespace Bot.Managers.ArmyManagement.Tactics.SneakAttack;

public partial class SneakAttackTactic {
    public class EngageState : SneakAttackState {
        private bool _goToNextState = false;

        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            return true;
        }

        protected override bool TryTransitioning() {
            if (_goToNextState) {
                StateMachine.TransitionTo(new FightState());
                return true;
            }

            return false;
        }

        protected override void Execute() {
            UnburrowUnderlings(StateMachine._army);

            _goToNextState = true;
        }
    }
}
