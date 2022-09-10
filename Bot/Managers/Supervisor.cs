using System.Collections.Generic;
using Bot.Builds;

namespace Bot.Managers;

public abstract class Supervisor {
    public abstract IEnumerable<BuildFulfillment> BuildFulfillments { get; }

    public HashSet<Unit> SupervisedUnits { get; } = new HashSet<Unit>();

    protected abstract IAssigner Assigner { get; }
    protected abstract IReleaser Releaser { get; }

    public void OnFrame() {
        Supervise();
    }

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

            Assigner.Assign(unit);
        }
        else {
            Logger.Error("({0}) Trying to assign {1} that's already assigned to us", this, unit);
        }
    }

    public void Release(Unit unit) {
        if (SupervisedUnits.Remove(unit)) {
            unit.Supervisor = null;

            unit.Stop();

            Releaser.Release(unit);
        }
        else {
            Logger.Error("({0}) Trying to release {1} that's not ours", this, unit);
        }
    }

    public abstract void Retire();
}
