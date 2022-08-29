using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Bot.Builds;
using Bot.GameData;
using Bot.GameSense;
using Bot.UnitModules;

namespace Bot.Managers;

public class CreepManager: Manager {
    public override IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();

    public static CreepManager Create() {
        var manager = new CreepManager();
        manager.Init();

        return manager;
    }

    private CreepManager() {}

    protected override IAssigner CreateAssigner() {
        return null;
    }

    protected override IDispatcher CreateDispatcher() {
        return null;
    }

    protected override IReleaser CreateReleaser() {
        return null;
    }

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
}
