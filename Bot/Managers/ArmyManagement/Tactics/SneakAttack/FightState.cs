using System.Collections.Generic;

namespace Bot.Managers.ArmyManagement.Tactics.SneakAttack;

public partial class SneakAttackTactic {
    public class FightState: SneakAttackState {
        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            return false;
        }

        protected override bool TryTransitioning() {
            throw new System.NotImplementedException();
        }

        protected override void Execute() {
            throw new System.NotImplementedException();
        }
    }
}
