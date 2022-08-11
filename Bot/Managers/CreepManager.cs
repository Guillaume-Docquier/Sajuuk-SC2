using System.Collections.Generic;
using System.Linq;
using Bot.GameData;
using Bot.GameSense;
using Bot.UnitModules;

namespace Bot.Managers;

public class CreepManager: IManager {
    public IEnumerable<BuildOrders.BuildStep> BuildStepRequests => Enumerable.Empty<BuildOrders.BuildStep>();

    public void OnFrame() {
        Controller.GetUnits(UnitsTracker.NewOwnedUnits, Units.CreepTumor)
            .ToList()
            .ForEach(TumorCreepSpreadModule.Install);
    }

    public void Release(Unit unit) {}

    public void Retire() {}

    public void ReportUnitDeath(Unit deadUnit) {}
}
