using System.Collections.Generic;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public class DefensiveUnitsControl : AggregateUnitsControl {
    public DefensiveUnitsControl(
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        IRegionsEvaluationsTracker regionsEvaluationsTracker
    ) : base(new List<IUnitsControl>
        {
            new BurrowHealing(unitsTracker, terrainTracker, regionsTracker, regionsEvaluationsTracker),
            new DisengagementKiting(unitsTracker),
        }) {}
}
