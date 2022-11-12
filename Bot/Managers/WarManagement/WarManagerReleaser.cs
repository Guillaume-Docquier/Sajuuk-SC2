using Bot.UnitModules;

namespace Bot.Managers.WarManagement;

public partial class WarManager {
    private class WarManagerReleaser: Releaser<WarManager> {
        public WarManagerReleaser(WarManager client) : base(client) {}

        public override void Release(Unit unit) {
            Logger.Debug("({0}) Released {1}", Client, unit);

            Client._soldiers.Remove(unit);
            UnitModule.Uninstall<ChangelingTargetingModule>(unit);
        }
    }
}
