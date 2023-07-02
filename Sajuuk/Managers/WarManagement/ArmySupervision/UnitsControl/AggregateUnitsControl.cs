using System.Collections.Generic;
using System.Linq;

namespace Sajuuk.Managers.WarManagement.ArmySupervision.UnitsControl;

public abstract class AggregateUnitsControl : IUnitsControl {
    private readonly List<IUnitsControl> _unitsControls;

    protected AggregateUnitsControl(List<IUnitsControl> unitsControls) {
        _unitsControls = unitsControls;
    }

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
