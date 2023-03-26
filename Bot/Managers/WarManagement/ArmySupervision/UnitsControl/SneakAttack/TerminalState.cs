using System.Collections.Generic;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttack;

public partial class SneakAttackUnitsControl {
    public class TerminalState: SneakAttackState {
        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            return false;
        }

        protected override void Execute() {
            Logger.Error("TerminalState should never be executed");
            NextState = new InactiveState();
        }
    }
}
