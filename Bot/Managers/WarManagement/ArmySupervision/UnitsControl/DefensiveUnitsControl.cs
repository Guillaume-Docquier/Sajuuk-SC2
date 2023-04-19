using System.Collections.Generic;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public class DefensiveUnitsControl : AggregateUnitsControl {
    public DefensiveUnitsControl()
        : base(new List<IUnitsControl>
        {
            new BurrowHealing(),
            new DisengagementKiting(),
        }) {}
}
