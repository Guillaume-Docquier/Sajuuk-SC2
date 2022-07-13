using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.UnitModules;

public class DebugLocationModule: IUnitModule {
    public const string Tag = "debug-location";

    private readonly Unit _unit;
    private readonly Color _color;

    public static void Install(Unit worker, Color color = null) {
        worker.Modules.Add(Tag, new DebugLocationModule(worker, color));
    }

    private DebugLocationModule(Unit unit, Color color = null) {
        _unit = unit;
        _color = color ?? Colors.White;
    }

    public void Execute() {
        Debugger.AddSphere(_unit, _color);
    }
}
