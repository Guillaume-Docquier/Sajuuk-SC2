using System.Collections.Generic;

namespace Bot.UnitModules;

public class CapacityModule: IUnitModule, IWatchUnitsDie {
    public const string Tag = "capacity";

    private readonly int _maxCapacity;
    public readonly List<Unit> AssignedUnits = new List<Unit>();

    public int AvailableCapacity => _maxCapacity - AssignedUnits.Count;

    public static void Install(Unit unit, int maxCapacity) {
        unit.Modules.Add(Tag, new CapacityModule(maxCapacity));
    }

    public static CapacityModule Uninstall(Unit unit) {
        var module = GetFrom(unit);
        unit.Modules.Remove(Tag);

        return module;
    }

    public static CapacityModule GetFrom(Unit unit) {
        if (unit.Modules.TryGetValue(Tag, out var module)) {
            return module as CapacityModule;
        }

        return null;
    }

    private CapacityModule(int maxCapacity) {
        _maxCapacity = maxCapacity;
    }

    public void Assign(Unit unit) {
        Assign(new List<Unit> { unit });
    }

    public void Assign(List<Unit> units) {
        units.ForEach(unit => unit.AddDeathWatcher(this));
        AssignedUnits.AddRange(units);
    }

    public void Release(Unit unitToRelease) {
        unitToRelease.RemoveDeathWatcher(this);
        AssignedUnits.Remove(unitToRelease);
    }

    public void Execute() {
        // Nothing to do
    }

    public void ReportUnitDeath(Unit deadUnit) {
        AssignedUnits.Remove(deadUnit);
    }
}
