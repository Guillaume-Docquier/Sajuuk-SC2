using System;
using System.Collections.Generic;
using Bot.Builds;

namespace Bot.Managers;

public abstract class Supervisor {
    public abstract IEnumerable<BuildFulfillment> BuildFulfillments { get; }

    public HashSet<Unit> SupervisedUnits { get; } = new HashSet<Unit>();

    private bool _isInitialized = false;
    private IAssigner _assigner;
    private IReleaser _releaser;

    /// <summary>
    /// Supervisors implementers need to call init before doing anything
    /// It is highly recommended to provide a static factory method to handle initialization and a private constructor
    /// </summary>
    protected void Init() {
        _assigner = CreateAssigner();
        _releaser = CreateReleaser();

        _isInitialized = true;
    }

    public void OnFrame() {
        if (!_isInitialized) {
            throw new InvalidOperationException($"({this}) was not initialized before use");
        }

        Supervise();
    }

    protected abstract IAssigner CreateAssigner();
    protected abstract IReleaser CreateReleaser();

    protected abstract void Supervise();

    public void Assign(IEnumerable<Unit> units) {
        foreach (var unit in units) {
            Assign(unit);
        }
    }

    // TODO GD Rename to Supervise(unit)?
    public void Assign(Unit unit) {
        if (SupervisedUnits.Add(unit)) {
            unit.Supervisor = this;

            _assigner.Assign(unit);
        }
        else {
            Logger.Error("({0}) Trying to assign {1} that's already assigned to us", this, unit);
        }
    }

    public void Release(Unit unit) {
        if (SupervisedUnits.Remove(unit)) {
            unit.Supervisor = null;

            unit.Stop();

            _releaser.Release(unit);
        }
        else {
            Logger.Error("({0}) Trying to release {1} that's not ours", this, unit);
        }
    }

    public abstract void Retire();
}
