namespace Bot.Managers;

public partial class WarManager {
    private class WarManagerDispatcher: Dispatcher<WarManager> {
        public WarManagerDispatcher(WarManager client) : base(client) {}

        public override void Dispatch(Unit unit) {
            Logger.Debug("({0}) Dispatched {1}", Client, unit);

            if (unit.IsFlying) {
                Client._airArmySupervisor.Assign(unit);
            }
            else {
                Client._groundArmySupervisor.Assign(unit);
            }
        }
    }
}
