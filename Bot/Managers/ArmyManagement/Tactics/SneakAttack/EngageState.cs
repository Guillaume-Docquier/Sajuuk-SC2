using System.Collections.Generic;
using System.Linq;
using Bot.GameData;

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

        private static void UnburrowUnderlings(IEnumerable<Unit> army) {
            foreach (var soldier in army.Where(soldier => soldier.RawUnitData.IsBurrowed)) {
                soldier.UseAbility(Abilities.BurrowRoachUp);
            }
        }
    }
}
