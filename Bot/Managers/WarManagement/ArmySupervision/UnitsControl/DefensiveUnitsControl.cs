using System.Collections.Generic;
using Bot.GameSense;
using Bot.MapKnowledge;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public class DefensiveUnitsControl : AggregateUnitsControl {
    public DefensiveUnitsControl(IUnitsTracker unitsTracker, IMapAnalyzer mapAnalyzer, IRegionAnalyzer regionAnalyzer)
        : base(new List<IUnitsControl>
        {
            new BurrowHealing(unitsTracker, mapAnalyzer, regionAnalyzer),
            new DisengagementKiting(unitsTracker),
        }) {}
}
