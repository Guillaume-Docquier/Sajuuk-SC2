using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.UnitModules;

public class DebugLocationModule: IUnitModule {
    public const string Tag = "debug-location-module";

    private readonly Unit _unit;
    private readonly Color _color;

    public static void Install(Unit unit, Color color = null) {
        if (UnitModule.PreInstallCheck(Tag, unit)) {
            unit.Modules.Add(Tag, new DebugLocationModule(unit, color));
        }
    }

    private DebugLocationModule(Unit unit, Color color = null) {
        _unit = unit;
        _color = color ?? Colors.White;
    }

    public void Execute() {
        GraphicalDebugger.AddSphere(_unit, _color);
    }
}
