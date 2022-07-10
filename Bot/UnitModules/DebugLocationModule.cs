using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.UnitModules;

public class DebugLocationModule: IUnitModule {
    private readonly Unit _unit;
    private readonly Color _color;

    public DebugLocationModule(Unit unit, Color color = null) {
        _unit = unit;
        _color = color ?? Colors.White;
    }

    public void Execute() {
        Debugger.AddSphere(_unit, _color);
    }
}
