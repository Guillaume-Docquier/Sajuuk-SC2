using System.Collections.Generic;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public class OffensiveUnitsControl : AggregateUnitsControl {
    public OffensiveUnitsControl(IUnitsControlFactory unitsControlFactory)
        : base(new List<IUnitsControl> {
            unitsControlFactory.CreateMineralWalkKiting(),
            unitsControlFactory.CreateSneakAttack(),
            unitsControlFactory.CreateBurrowHealing(),
            unitsControlFactory.CreateStutterStep(),
        }) {}
}
