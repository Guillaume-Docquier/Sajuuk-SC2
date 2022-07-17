using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.UnitModules;

public class DebugLocationModule: IUnitModule {
    public const string Tag = "debug-location";

    private readonly Unit _unit;
    private readonly Color _color;

    public static void Install(Unit unit, Color color = null) {
        unit.Modules.Add(Tag, new DebugLocationModule(unit, color));
    }

    public static DebugLocationModule Uninstall(Unit unit) {
        var module = GetFrom(unit);
        if (module != null) {
            unit.Modules.Remove(Tag);
        }

        return module;
    }

    public static DebugLocationModule GetFrom(Unit worker) {
        if (worker == null) {
            return null;
        }

        if (worker.Modules.TryGetValue(Tag, out var module)) {
            return module as DebugLocationModule;
        }

        return null;
    }

    private DebugLocationModule(Unit unit, Color color = null) {
        _unit = unit;
        _color = color ?? Colors.White;
    }

    public void Execute() {
        GraphicalDebugger.AddSphere(_unit, _color);
    }
}
