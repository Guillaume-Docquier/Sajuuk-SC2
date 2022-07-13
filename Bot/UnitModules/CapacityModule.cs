using System.Collections.Generic;
using System.Linq;

namespace Bot.UnitModules;

public class CapacityModule: IUnitModule, IWatchUnitsDie {
    public const string Tag = "capacity";

    private readonly int _maxCapacity;
    private readonly List<Unit> _assignedUnits = new List<Unit>();

    public int AvailableCapacity => _maxCapacity - _assignedUnits.Count;

    public static void Install(Unit worker, int maxCapacity) {
        worker.Modules.Add(Tag, new CapacityModule(maxCapacity));
    }

    public static int GetAvailableCapacity(Unit unit) {
        var capacityModule = unit.Modules[Tag] as CapacityModule;

        return capacityModule!.AvailableCapacity;
    }

    public static void Assign(Unit toUnit, Unit unit) {
        var capacityModule = toUnit.Modules[Tag] as CapacityModule;

        capacityModule!.Assign(unit);
    }

    public static void Release(Unit fromUnit, Unit unit) {
        var capacityModule = fromUnit.Modules[Tag] as CapacityModule;

        capacityModule!.Release(unit);
    }

    private CapacityModule(int maxCapacity) {
        _maxCapacity = maxCapacity;
    }

    public void Assign(Unit unit) {
        Assign(new List<Unit> { unit });
    }

    public void Assign(List<Unit> units) {
        units.ForEach(unit => unit.AddDeathWatcher(this));
        _assignedUnits.AddRange(units);
    }

    public void Release(Unit unitToRelease) {
        unitToRelease.RemoveDeathWatcher(this);
        _assignedUnits.Remove(unitToRelease);
    }

    public void Execute() {
        // Nothing to do
    }

    public void ReportUnitDeath(Unit deadUnit) {
        _assignedUnits.Remove(deadUnit);
    }
}
