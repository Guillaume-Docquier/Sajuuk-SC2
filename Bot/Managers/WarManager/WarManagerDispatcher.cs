namespace Bot.Managers;

public partial class WarManager {
    private class WarManagerDispatcher: IDispatcher {
        private readonly WarManager _manager;

        public WarManagerDispatcher(WarManager manager) {
            _manager = manager;
        }

        public void Dispatch(Unit unit) {
            Logger.Debug("({0}) Dispatched {1}", _manager, unit);

            if (unit.IsFlying) {
                _manager._airArmySupervisor.Assign(unit);
            }
            else {
                _manager._groundArmySupervisor.Assign(unit);
            }
        }
    }
}
