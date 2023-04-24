using System.Collections.Generic;
using System.Linq;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;

public partial class SneakAttack {
    public class EngageState : SneakAttackState {
        private readonly IUnitsTracker _unitsTracker;

        public EngageState(IUnitsTracker unitsTracker) {
            _unitsTracker = unitsTracker;
        }

        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            return true;
        }

        protected override void Execute() {
            UnburrowUnderlings(Context._army);

            NextState = new TerminalState(_unitsTracker);
        }

        private static void UnburrowUnderlings(IEnumerable<Unit> army) {
            foreach (var soldier in army.Where(soldier => soldier.IsBurrowed)) {
                soldier.UseAbility(Abilities.BurrowRoachUp);
            }
        }
    }
}
