namespace Bot.Managers.WarManagement.States.Finisher;

public class FinisherDispatcher : Dispatcher<FinisherBehaviour> {
    public FinisherDispatcher(FinisherBehaviour client) : base(client) {}

    public override void Dispatch(Unit unit) {
        Logger.Debug("({0}) Dispatched {1}", Client, unit);

        if (unit.IsFlying) {
            // For now we only use flying units when we need to because Terran scums can fly
            Client.TerranFinisherSupervisor.Assign(unit);
        }
        else {
            Client.AttackSupervisor.Assign(unit);
        }
    }
}
