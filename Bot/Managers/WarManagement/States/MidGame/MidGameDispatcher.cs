namespace Bot.Managers.WarManagement.States.MidGame;

public class MidGameDispatcher : Dispatcher<MidGameBehaviour> {
    public MidGameDispatcher(MidGameBehaviour client) : base(client) {}

    public override void Dispatch(Unit unit) {
        Logger.Debug("({0}) Dispatched {1}", Client, unit);
        // TODO GD Improve this
        if (Client.Stance.HasFlag(Stance.Attack)) {
            if (unit.IsFlying) {
                // For now we only use flying units when we need to because Terran scums can fly
                Client.TerranFinisherSupervisor.Assign(unit);
            }
            else {
                Client.AttackSupervisor.Assign(unit);
            }
        }
        else {
            Client.DefenseSupervisor.Assign(unit);
        }
    }
}
