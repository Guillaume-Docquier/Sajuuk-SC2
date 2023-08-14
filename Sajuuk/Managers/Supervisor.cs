using System.Collections.Generic;
using Sajuuk.Builds;

namespace Sajuuk.Managers;

public abstract class Supervisor {
    public abstract IEnumerable<IFulfillableBuildRequest> BuildRequests { get; }

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
    }

    public void Release(Unit unit) {
        if (SupervisedUnits.Remove(unit)) {
            unit.Supervisor = null;

            // TODO GD Test that stop is called
            unit.Stop();

            Releaser.Release(unit);
        }
        else {
            // TODO GD This happens with extractors when shit hits the fan
            Logger.Error("({0}) Trying to release {1} that's not ours", this, unit);
        }
    }

    // TODO GD Retire always means at least releasing all the units
    // We could abstract this?
    public abstract void Retire();
}
