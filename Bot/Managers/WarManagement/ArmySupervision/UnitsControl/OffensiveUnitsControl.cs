using System.Collections.Generic;
using Bot.GameSense;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl.StutterStepUnitsControl;
using Bot.MapKnowledge;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public class OffensiveUnitsControl : AggregateUnitsControl {
    public OffensiveUnitsControl(IUnitsTracker unitsTracker, IMapAnalyzer mapAnalyzer, IRegionAnalyzer regionAnalyzer)
        : base(new List<IUnitsControl>
        {
            new MineralWalkKiting(unitsTracker, mapAnalyzer),
            new SneakAttack(unitsTracker, mapAnalyzer),
            new BurrowHealing(unitsTracker, mapAnalyzer, regionAnalyzer),
            new StutterStep(),
        }) {}
}
