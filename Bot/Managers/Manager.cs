using System.Collections.Generic;
using Bot.Builds;

namespace Bot.Managers;

public abstract class Manager: IWatchUnitsDie {
    public abstract IEnumerable<BuildFulfillment> BuildFulfillments { get; }

    public HashSet<Unit> ManagedUnits { get; } = new HashSet<Unit>();

    protected abstract IAssigner Assigner { get; }
    protected abstract IDispatcher Dispatcher { get; }
    protected abstract IReleaser Releaser { get; }

    public void OnFrame() {
        RecruitmentPhase();
        DispatchPhase();
        ManagementPhase();
    }

    // TODO GD Review this system, dispatch often is empty/simple and ManagementPhase often does dispatching
    protected abstract void RecruitmentPhase();
    protected abstract void DispatchPhase();
    protected abstract void ManagementPhase();

    public void Assign(IEnumerable<Unit> units) {
        foreach (var unit in units) {
            Assign(unit);
        }
    }

    // TODO GD Rename to Manage(unit)?
    public void Assign(Unit unit) {
        if (ManagedUnits.Add(unit)) {
            unit.Manager = this;
            unit.AddDeathWatcher(this);

            Assigner.Assign(unit);
        }
        else {
            Logger.Error("({0}) Trying to assign {1} that's already assigned to us", this, unit);
        }
    }

    public void Dispatch(IEnumerable<Unit> units) {
        foreach (var unit in units) {
            Dispatch(unit);
        }
    }

    public void Dispatch(Unit unit) {
        if (ManagedUnits.Contains(unit)) {
            Dispatcher.Dispatch(unit);
        }
        else {
            Logger.Error("({0}) Trying to dispatch {1} that's not ours", this, unit);
        }
    }

    public void Release(IEnumerable<Unit> units) {
        foreach (var unit in units) {
            Release(unit);
        }
    }

    public void Release(Unit unit) {
        if (ManagedUnits.Remove(unit)) {
            unit.Manager = null;
            unit.RemoveDeathWatcher(this);

            // TODO GD Test that stop is called
            unit.Stop();

            Releaser.Release(unit);
            unit.Supervisor?.Release(unit);
        }
        else {
            Logger.Error("({0}) Trying to release {1} that's not ours", this, unit);
        }
    }

    public void ReportUnitDeath(Unit deadUnit) {
        if (ManagedUnits.Contains(deadUnit)) {
            Release(deadUnit);
        }
        else {
            Logger.Error("({0}) Reported death of {1}, but we don't manage this unit", this, deadUnit);
        }
    }
}
