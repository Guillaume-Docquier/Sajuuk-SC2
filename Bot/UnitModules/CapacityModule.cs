using System.Collections.Generic;

namespace Bot.UnitModules;

public class CapacityModule: IUnitModule, IWatchUnitsDie {
    private readonly int _maxCapacity;
    private readonly List<Unit> _assignedUnits = new List<Unit>();

    public int AvailableCapacity => _maxCapacity - _assignedUnits.Count;

    public CapacityModule(int maxCapacity) {
        _maxCapacity = maxCapacity;
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
