using System.Collections.Generic;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameSense;
using Bot.GameSense.RegionsEvaluationsTracking;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl.StutterStepUnitsControl;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public class OffensiveUnitsControl : AggregateUnitsControl {
    public OffensiveUnitsControl(
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        IRegionsEvaluationsTracker regionsEvaluationsTracker,
        IGraphicalDebugger graphicalDebugger
    ) : base(new List<IUnitsControl>
        {
            new MineralWalkKiting(unitsTracker, terrainTracker, graphicalDebugger),
            new SneakAttack(unitsTracker, terrainTracker, graphicalDebugger),
            new BurrowHealing(unitsTracker, terrainTracker, regionsTracker, regionsEvaluationsTracker),
            new StutterStep(graphicalDebugger),
        }) {}
}
