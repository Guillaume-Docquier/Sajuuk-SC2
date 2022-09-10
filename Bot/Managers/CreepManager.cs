using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.GameData;
using Bot.GameSense;
using Bot.UnitModules;

namespace Bot.Managers;

public class CreepManager: Manager {
    public override IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();

    protected override IAssigner Assigner { get; } = new DummyAssigner();
    protected override IDispatcher Dispatcher { get; } = new DummyDispatcher();
    protected override IReleaser Releaser { get; } = new DummyReleaser();

    protected override void AssignUnits() {}

    protected override void DispatchUnits() {}

    protected override void Manage() {
        foreach (var creepTumor in Controller.GetUnits(UnitsTracker.NewOwnedUnits, Units.CreepTumor)) {
            TumorCreepSpreadModule.Install(creepTumor);
        }
    }

    public override string ToString() {
        return "CreepManager";
    }

    private class DummyAssigner : IAssigner {
        public void Assign(Unit unit) {}
    }

    private class DummyDispatcher : IDispatcher {
        public void Dispatch(Unit unit) {}
    }

    private class DummyReleaser : IReleaser {
        public void Release(Unit unit) {}
    }
}
