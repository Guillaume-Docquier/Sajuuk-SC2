namespace Bot.UnitModules;

public class KitingModule: IUnitModule {
    public const string Tag = "kiting-module";

    private readonly Unit _unit;

    public static void Install(Unit unit) {
        unit.Modules.Add(Tag, new KitingModule(unit));
    }

    public static KitingModule Uninstall(Unit unit) {
        var module = GetFrom(unit);
        if (module != null) {
            unit.Modules.Remove(Tag);
        }

        return module;
    }

    public static KitingModule GetFrom(Unit unit) {
        if (unit == null) {
            return null;
        }

        if (unit.Modules.TryGetValue(Tag, out var module)) {
            return module as KitingModule;
        }

        return null;
    }

    private KitingModule(Unit unit) {
        _unit = unit;
    }

    public void Execute() {
        throw new System.NotImplementedException();
    }
}
