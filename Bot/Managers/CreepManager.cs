using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.GameData;
using Bot.GameSense;
using Bot.UnitModules;

namespace Bot.Managers;

public class CreepManager: UnitlessManager {
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IUnitsTracker _unitsTracker;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();

    public CreepManager(IVisibilityTracker visibilityTracker, IUnitsTracker unitsTracker) {
        _visibilityTracker = visibilityTracker;
        _unitsTracker = unitsTracker;
    }

    protected override void ManagementPhase() {
        foreach (var creepTumor in Controller.GetUnits(_unitsTracker.NewOwnedUnits, Units.CreepTumor)) {
            TumorCreepSpreadModule.Install(_visibilityTracker, creepTumor);
        }
    }

    public override string ToString() {
        return "CreepManager";
    }
}
