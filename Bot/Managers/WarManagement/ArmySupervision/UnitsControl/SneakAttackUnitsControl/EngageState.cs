using System.Collections.Generic;
using System.Linq;
using Bot.GameData;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;

public partial class SneakAttack {
    public class EngageState : SneakAttackState {
        private readonly ISneakAttackStateFactory _sneakAttackStateFactory;

        public EngageState(
            ISneakAttackStateFactory sneakAttackStateFactory
        ) {
            _sneakAttackStateFactory = sneakAttackStateFactory;
        }

        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            return true;
        }

        protected override void Execute() {
            UnburrowUnderlings(Context._army);

            NextState = _sneakAttackStateFactory.CreateTerminalState();
        }

        private static void UnburrowUnderlings(IEnumerable<Unit> army) {
            foreach (var soldier in army.Where(soldier => soldier.IsBurrowed)) {
                soldier.UseAbility(Abilities.BurrowRoachUp);
            }
        }
    }
}
