using System.Collections.Generic;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl.StutterStepUnitsControl;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public class OffensiveUnitsControl : AggregateUnitsControl {
    public OffensiveUnitsControl()
        : base(new List<IUnitsControl>
        {
            new MineralWalkKiting(),
            new SneakAttack(),
            new BurrowHealing(),
            new StutterStep(),
        }) {}
}
