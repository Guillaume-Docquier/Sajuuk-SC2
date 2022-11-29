namespace Bot.Managers.WarManagement.States.EarlyGame;

public class EarlyGameDispatcher : Dispatcher<EarlyGameBehaviour> {
    public EarlyGameDispatcher(EarlyGameBehaviour client) : base(client) {}

    public override void Dispatch(Unit unit) {
        Logger.Debug("({0}) Dispatched {1}", Client, unit);
        Client.DefenseSupervisor.Assign(unit);
    }
}
