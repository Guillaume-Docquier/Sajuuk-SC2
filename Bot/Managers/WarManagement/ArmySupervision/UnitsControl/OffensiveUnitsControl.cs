using System.Collections.Generic;
using Bot.GameSense;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl.StutterStepUnitsControl;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public class OffensiveUnitsControl : AggregateUnitsControl {
    public OffensiveUnitsControl(IUnitsTracker unitsTracker)
        : base(new List<IUnitsControl>
        {
            new MineralWalkKiting(unitsTracker),
            new SneakAttack(unitsTracker),
            new BurrowHealing(unitsTracker),
            new StutterStep(),
        }) {}
}
