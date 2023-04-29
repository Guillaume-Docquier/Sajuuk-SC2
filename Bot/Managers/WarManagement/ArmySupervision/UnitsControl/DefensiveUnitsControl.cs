using System.Collections.Generic;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
using Bot.MapKnowledge;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public class DefensiveUnitsControl : AggregateUnitsControl {
    public DefensiveUnitsControl(IUnitsTracker unitsTracker, IMapAnalyzer mapAnalyzer, IRegionAnalyzer regionAnalyzer, IRegionsEvaluationsTracker regionsEvaluationsTracker)
        : base(new List<IUnitsControl>
        {
            new BurrowHealing(unitsTracker, mapAnalyzer, regionAnalyzer, regionsEvaluationsTracker),
            new DisengagementKiting(unitsTracker),
        }) {}
}
