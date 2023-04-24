using System.Collections.Generic;
using Bot.GameSense;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public class DefensiveUnitsControl : AggregateUnitsControl {
    public DefensiveUnitsControl(IUnitsTracker unitsTracker)
        : base(new List<IUnitsControl>
        {
            new BurrowHealing(unitsTracker),
            new DisengagementKiting(unitsTracker),
        }) {}
}
