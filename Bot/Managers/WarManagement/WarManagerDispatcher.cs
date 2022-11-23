namespace Bot.Managers.WarManagement;

public partial class WarManager {
    private class WarManagerDispatcher: Dispatcher<WarManager> {
        public WarManagerDispatcher(WarManager client) : base(client) {}

        public override void Dispatch(Unit unit) {
            Logger.Debug("({0}) Dispatched {1}", Client, unit);
            // TODO GD Improve this
            if (Client._isAttacking) {
                if (unit.IsFlying) {
                    Client._airAttackSupervisor.Assign(unit);
                }
                else {
                    Client._groundAttackSupervisor.Assign(unit);
                }
            }
            else {
                Client._defenseSupervisor.Assign(unit);
            }
        }
    }
}
