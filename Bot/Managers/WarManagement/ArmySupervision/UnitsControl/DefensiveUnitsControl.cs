using System.Collections.Generic;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public class DefensiveUnitsControl : AggregateUnitsControl {
    public DefensiveUnitsControl(IUnitsControlFactory unitsControlFactory)
        : base(new List<IUnitsControl> {
            unitsControlFactory.CreateBurrowHealing(),
            unitsControlFactory.CreateDisengagementKiting(),
        }) {}
}
