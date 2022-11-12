using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.GameData;
using Bot.GameSense;
using Bot.UnitModules;

namespace Bot.Managers;

public class CreepManager: UnitlessManager {
    public override IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();

    protected override void ManagementPhase() {
        foreach (var creepTumor in Controller.GetUnits(UnitsTracker.NewOwnedUnits, Units.CreepTumor)) {
            TumorCreepSpreadModule.Install(creepTumor);
        }
    }

    public override string ToString() {
        return "CreepManager";
    }
}
