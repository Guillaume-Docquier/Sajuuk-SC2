using System.Collections.Generic;

namespace Sajuuk.Managers.WarManagement.ArmySupervision.UnitsControl;

public class DefensiveUnitsControl : AggregateUnitsControl {
    public DefensiveUnitsControl(IUnitsControlFactory unitsControlFactory)
        : base(new List<IUnitsControl> {
            unitsControlFactory.CreateBurrowHealing(),
            unitsControlFactory.CreateDisengagementKiting(),
        }) {}
}
