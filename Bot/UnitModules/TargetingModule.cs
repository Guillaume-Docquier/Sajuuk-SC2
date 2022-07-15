using System.Numerics;

namespace Bot.UnitModules;

public class TargetingModule: IUnitModule {
    public const string Tag = "targeting-module";

    private readonly Unit _unit;
    private readonly Vector3 _target;

    public static void Install(Unit unit, Vector3 target) {
        unit.Modules.Add(Tag, new TargetingModule(unit, target));
    }

    public static TargetingModule Uninstall(Unit unit) {
        var module = GetFrom(unit);
        unit.Modules.Remove(Tag);

        return module;
    }

    public static TargetingModule GetFrom(Unit unit) {
        if (unit.Modules.TryGetValue(Tag, out var module)) {
            return module as TargetingModule;
        }

        return null;
    }

    private TargetingModule(Unit unit, Vector3 target) {
        _unit = unit;
        _target = target;
    }

    public void Execute() {
        throw new System.NotImplementedException();
    }
}
