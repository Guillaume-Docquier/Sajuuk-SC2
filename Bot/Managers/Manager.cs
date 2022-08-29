using System;
using System.Collections.Generic;
using Bot.Builds;

namespace Bot.Managers;

public abstract class Manager: IWatchUnitsDie {
    public abstract IEnumerable<BuildFulfillment> BuildFulfillments { get; }

    public HashSet<Unit> ManagedUnits { get; } = new HashSet<Unit>();

    private bool _isInitialized = false;
    private IAssigner _assigner;
    private IDispatcher _dispatcher;
    private IReleaser _releaser;

    protected void Init() {
        _assigner = CreateAssigner();
        _dispatcher = CreateDispatcher();
        _releaser = CreateReleaser();

        _isInitialized = true;
    }

    public void OnFrame() {
        if (!_isInitialized) {
            throw new InvalidOperationException($"({this}) was not initialized before use");
        }

        AssignUnits();
        DispatchUnits();
        Manage();
    }

    protected abstract IAssigner CreateAssigner();
    protected abstract IDispatcher CreateDispatcher();
    protected abstract IReleaser CreateReleaser();

    protected abstract void AssignUnits();
    protected abstract void DispatchUnits();
    protected abstract void Manage();

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

            _assigner?.Assign(unit);
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
            _dispatcher?.Dispatch(unit);
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

            unit.Stop();

            _releaser?.Release(unit);
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
