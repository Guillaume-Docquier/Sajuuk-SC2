using Bot.UnitModules;

namespace Bot.Managers;

public partial class WarManager {
    private class WarManagerReleaser: IReleaser {
        private readonly WarManager _manager;

        public WarManagerReleaser(WarManager manager) {
            _manager = manager;
        }

        public void Release(Unit unit) {
            _manager._soldiers.Remove(unit);
            UnitModule.Uninstall<ChangelingTargetingModule>(unit);
        }
    }
}
