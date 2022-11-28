using System.Linq;

namespace Bot.Managers.WarManagement.States.MidGame;

public class MidGameDispatchPhaseStrategy : WarManagerStrategy {
    public MidGameDispatchPhaseStrategy(WarManager context) : base(context) {}

    public override void Execute() {
        WarManager.Dispatch(WarManager.ManagedUnits.Where(soldier => soldier.Supervisor == null));
    }
}
