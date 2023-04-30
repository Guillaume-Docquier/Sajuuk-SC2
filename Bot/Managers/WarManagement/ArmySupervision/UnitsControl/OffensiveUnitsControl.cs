using System.Collections.Generic;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl.StutterStepUnitsControl;
using Bot.MapAnalysis.RegionAnalysis;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public class OffensiveUnitsControl : AggregateUnitsControl {
    public OffensiveUnitsControl(IUnitsTracker unitsTracker, ITerrainTracker terrainTracker, IRegionsTracker regionsTracker, IRegionsEvaluationsTracker regionsEvaluationsTracker)
        : base(new List<IUnitsControl>
        {
            new MineralWalkKiting(unitsTracker, terrainTracker),
            new SneakAttack(unitsTracker, terrainTracker),
            new BurrowHealing(unitsTracker, terrainTracker, regionsTracker, regionsEvaluationsTracker),
            new StutterStep(),
        }) {}
}
