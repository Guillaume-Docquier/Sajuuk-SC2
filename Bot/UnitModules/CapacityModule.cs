using System.Collections.Generic;

namespace Bot.UnitModules;

public class CapacityModule: IUnitModule, IWatchUnitsDie {
    public const string GlobalTag = "capacity-module-tag";

    private readonly int _maxCapacity;
    private readonly List<Unit> _assignedUnits = new List<Unit>();

    public int AvailableCapacity => _maxCapacity - _assignedUnits.Count;

    public CapacityModule(int maxCapacity) {
        _maxCapacity = maxCapacity;
    }

    public static int GetAvailableCapacity(Unit unit) {
        var capacityModule = unit.Modules[GlobalTag] as CapacityModule;

        return capacityModule!.AvailableCapacity;
    }

    public static void Assign(Unit toUnit, Unit unit) {
        var capacityModule = toUnit.Modules[GlobalTag] as CapacityModule;

        capacityModule!.Assign(unit);
    }

    public static void Assign(Unit toUnit, List<Unit> units) {
        var capacityModule = toUnit.Modules[GlobalTag] as CapacityModule;

        capacityModule!.Assign(units);
    }

    public void Assign(Unit unit) {
        Assign(new List<Unit> { unit });
    }

    public void Assign(List<Unit> units) {
        units.ForEach(unit => unit.AddDeathWatcher(this));
        _assignedUnits.AddRange(units);
    }

    public void Release(List<Unit> units) {
        units.ForEach(unit => {
            unit.RemoveDeathWatcher(this);
            _assignedUnits.Remove(unit);
        });
    }

    public void Execute() {
        // Nothing to do
    }

    public void ReportUnitDeath(Unit deadUnit) {
        _assignedUnits.Remove(deadUnit);
    }
}
