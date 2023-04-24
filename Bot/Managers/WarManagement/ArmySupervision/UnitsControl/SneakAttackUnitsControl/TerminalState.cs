using System.Collections.Generic;
using Bot.GameSense;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;

public partial class SneakAttack {
    public class TerminalState: SneakAttackState {
        private readonly IUnitsTracker _unitsTracker;

        public TerminalState(IUnitsTracker unitsTracker) {
            _unitsTracker = unitsTracker;
        }

        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            return false;
        }

        protected override void Execute() {
            Logger.Error("TerminalState should never be executed");
            NextState = new InactiveState(_unitsTracker);
        }
    }
}
