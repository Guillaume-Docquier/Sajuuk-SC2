namespace Bot.Managers.WarManagement;

public partial class WarManager {
    private class WarManagerDispatcher: Dispatcher<WarManager> {
        public WarManagerDispatcher(WarManager client) : base(client) {}

        public override void Dispatch(Unit unit) {
            Logger.Debug("({0}) Dispatched {1}", Client, unit);
            // TODO GD Improve this
            if (Client._stance.HasFlag(Stance.Attack)) {
                if (unit.IsFlying) {
                    // For now we only use flying units when we need to because Terran scums can fly
                    Client._terranFinisherSupervisor.Assign(unit);
                }
                else {
                    Client._attackSupervisor.Assign(unit);
                }
            }
            else {
                Client._defenseSupervisor.Assign(unit);
            }
        }
    }
}
