using System.Linq;

namespace Bot.Managers.WarManagement.States.EarlyGame;

public class EarlyGameDispatchPhaseStrategy : WarManagerStrategy {
    public EarlyGameDispatchPhaseStrategy(WarManager context) : base(context) {}

    public override void Execute() {
        WarManager.Dispatch(WarManager.ManagedUnits.Where(soldier => soldier.Supervisor == null));
    }
}
