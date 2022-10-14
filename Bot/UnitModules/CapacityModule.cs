using System.Collections.Generic;
using Bot.ExtensionMethods;
using Bot.Wrapper;

namespace Bot.UnitModules;

public class CapacityModule: UnitModule, IWatchUnitsDie {
    public const string Tag = "CapacityModule";

    private readonly Unit _unit;
    private readonly int _maxCapacity;
    public readonly List<Unit> AssignedUnits = new List<Unit>();

    public int AvailableCapacity => _maxCapacity - AssignedUnits.Count;

    public static void Install(Unit unit, int maxCapacity) {
        if (PreInstallCheck(Tag, unit)) {
            unit.Modules.Add(Tag, new CapacityModule(unit, maxCapacity));
        }
    }

    private CapacityModule(Unit unit, int maxCapacity) {
        _unit = unit;
        _maxCapacity = maxCapacity;
    }

    protected override void DoExecute() {
        var color = AvailableCapacity switch
        {
            > 0 => Colors.Green,
              0 => Colors.Yellow,
            < 0 => Colors.Red,
        };

        Program.GraphicalDebugger.AddText($"{AssignedUnits.Count}/{_maxCapacity}", color: color, worldPos: _unit.Position.ToPoint(xOffset: -0.2f));
    }

    public void ReportUnitDeath(Unit deadUnit) {
        Release(deadUnit);
    }

    public void Assign(Unit unit) {
        Assign(new List<Unit> { unit });
    }

    public void Assign(List<Unit> units) {
        units.ForEach(unit => unit.AddDeathWatcher(this));
        AssignedUnits.AddRange(units);
    }

    public Unit ReleaseOne() {
        if (AssignedUnits.Count <= 0) {
            Logger.Error("Trying to release one unit from CapacityModule, but there's no assigned units");
            return null;
        }

        var unitToRelease = AssignedUnits[0];

        Release(unitToRelease);

        return unitToRelease;
    }

    public void Release(Unit unitToRelease) {
        unitToRelease.RemoveDeathWatcher(this);
        AssignedUnits.Remove(unitToRelease);
    }
}
