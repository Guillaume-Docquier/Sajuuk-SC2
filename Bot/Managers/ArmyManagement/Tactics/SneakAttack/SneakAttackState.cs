using System.Collections.Generic;
using Bot.StateManagement;

namespace Bot.Managers.ArmyManagement.Tactics.SneakAttack;

public abstract class SneakAttackState: State<SneakAttackTactic> {
    public abstract bool IsViable(IReadOnlyCollection<Unit> army);
}
