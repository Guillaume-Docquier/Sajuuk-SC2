using System.Collections.Generic;

namespace Bot.Managers.ArmyManagement.Tactics.SneakAttack;

public partial class SneakAttackTactic {
    public class EngageState : SneakAttackState {
        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            return true;
        }

        protected override void Execute() {
            UnburrowUnderlings(StateMachine._army);

            NextState = new TerminalState();
        }
    }
}
