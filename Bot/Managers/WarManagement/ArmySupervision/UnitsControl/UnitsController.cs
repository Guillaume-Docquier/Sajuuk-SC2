using System.Collections.Generic;
using System.Linq;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public class UnitsController : IUnitsControl {
    private readonly List<IUnitsControl> _unitsControls = new List<IUnitsControl>
    {
        new MineralWalkKiting(),
        new SneakAttack(),
        new BurrowHealing(),
    };

    public bool IsExecuting() {
        return _unitsControls.Any(unitsControl => unitsControl.IsExecuting());
    }

    public IReadOnlySet<Unit> Execute(IReadOnlySet<Unit> army) {
        return _unitsControls.Aggregate(army, (remainingArmy, unitsControl) => unitsControl.Execute(remainingArmy));
    }

    public void Reset(IReadOnlyCollection<Unit> army) {
        _unitsControls.ForEach(unitsControl => unitsControl.Reset(army));
    }
}
