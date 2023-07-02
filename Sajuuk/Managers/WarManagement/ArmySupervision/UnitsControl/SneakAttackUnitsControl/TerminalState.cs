using System.Collections.Generic;

namespace Sajuuk.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;

public partial class SneakAttack {
    public class TerminalState: SneakAttackState {
        private readonly ISneakAttackStateFactory _sneakAttackStateFactory;

        public TerminalState(
            ISneakAttackStateFactory sneakAttackStateFactory
        ) {
            _sneakAttackStateFactory = sneakAttackStateFactory;
        }

        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            return false;
        }

        protected override void Execute() {
            Logger.Error("TerminalState should never be executed");
            NextState = _sneakAttackStateFactory.CreateInactiveState();
        }
    }
}
