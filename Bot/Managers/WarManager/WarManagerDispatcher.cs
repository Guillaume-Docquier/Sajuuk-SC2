namespace Bot.Managers;

public partial class WarManager {
    private class WarManagerDispatcher: IDispatcher {
        private readonly WarManager _manager;

        public WarManagerDispatcher(WarManager manager) {
            _manager = manager;
        }

        public void Dispatch(Unit unit) {
            _manager._armySupervisor.Assign(unit);
        }
    }
}
