namespace Bot.Managers.WarManagement.States.MidGame;

public class MidGameDispatcher : Dispatcher<MidGameBehaviour> {
    public MidGameDispatcher(MidGameBehaviour client) : base(client) {}

    public override void Dispatch(Unit unit) {
        Logger.Debug($"({Client}) Dispatched {unit}");

        if (Client.Stance == Stance.Attack) {
            Client.AttackSupervisor.Assign(unit);
        }
        else {
            Client.DefenseSupervisor.Assign(unit);
        }
    }
}
