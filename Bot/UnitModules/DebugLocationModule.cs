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

    public static DebugLocationModule GetFrom(Unit unit) {
        return unit.Modules[Tag] as DebugLocationModule;
    }

    private DebugLocationModule(Unit unit, Color color = null) {
        _unit = unit;
        _color = color ?? Colors.White;
    }

    public void Execute() {
        Debugger.AddSphere(_unit, _color);
    }
}
