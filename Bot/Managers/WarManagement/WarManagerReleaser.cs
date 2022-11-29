using Bot.UnitModules;

namespace Bot.Managers.WarManagement;

public class WarManagerReleaser<T> : Releaser<T> {
    public WarManagerReleaser(T client) : base(client) {}

    public override void Release(Unit unit) {
        Logger.Debug("({0}) Released {1}", Client, unit);
        UnitModule.Uninstall<ChangelingTargetingModule>(unit);
    }
}
