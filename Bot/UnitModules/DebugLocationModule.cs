using Bot.Wrapper;

namespace Bot.UnitModules;

public class DebugLocationModule: IUnitModule {
    private readonly Unit _unit;

    public DebugLocationModule(Unit unit) {
        _unit = unit;
    }

    public void Execute() {
        if (Units.MineralFields.Contains(_unit.UnitType) || Units.GasGeysers.Contains(_unit.UnitType)) {
            //Debugger.AddSphere(_unit, _unit.Radius * 1.25f, Colors.Cyan);
        }
    }
}
