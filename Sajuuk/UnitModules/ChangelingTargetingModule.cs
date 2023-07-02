using System.Linq;
using Sajuuk.GameData;
using Sajuuk.GameSense;

namespace Sajuuk.UnitModules;

public class ChangelingTargetingModule: UnitModule {
    public const string ModuleTag = "ChangelingTargetingModule";

    private readonly IUnitsTracker _unitsTracker;

    private readonly Unit _unit;

    public ChangelingTargetingModule(
        IUnitsTracker unitsTracker,
        Unit unit
    ) : base(ModuleTag) {
        _unit = unit;
        _unitsTracker = unitsTracker;
    }

    protected override void DoExecute() {
        var changelings = _unitsTracker.GetUnits(_unitsTracker.EnemyUnits, Units.Changelings).ToList();
        if (changelings.Count <= 0) {
            return;
        }

        var closestChangelingInRange = changelings.FirstOrDefault(changeling => changeling.DistanceTo(_unit) <= _unit.MaxRange);
        if (closestChangelingInRange != null) {
            _unit.Attack(closestChangelingInRange);
        }
    }
}
