using Bot.Wrapper;

namespace Bot.UnitModules;

public class DebugLocationModule: IUnitModule {
    private readonly Unit _unit;

    public DebugLocationModule(Unit unit) {
        _unit = unit;
    }

    public void Execute() {
        Debugger.AddSphere(_unit, Colors.Cyan);
    }
}
