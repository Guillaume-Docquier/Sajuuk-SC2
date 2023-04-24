using System.Linq;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.UnitModules;

public class ChangelingTargetingModule: UnitModule {
    private readonly IUnitsTracker _unitsTracker;

    public const string Tag = "ChangelingTargetingModule";

    private readonly Unit _unit;

    private ChangelingTargetingModule(Unit unit, IUnitsTracker unitsTracker) {
        _unit = unit;
        _unitsTracker = unitsTracker;
    }

    public static void Install(Unit unit, IUnitsTracker unitsTracker) {
        if (PreInstallCheck(Tag, unit)) {
            unit.Modules.Add(Tag, new ChangelingTargetingModule(unit, unitsTracker));
        }
    }

    protected override void DoExecute() {
        var changelings = Controller.GetUnits(_unitsTracker.EnemyUnits, Units.Changelings).ToList();
        if (changelings.Count <= 0) {
            return;
        }

        var closestChangelingInRange = changelings.FirstOrDefault(changeling => changeling.DistanceTo(_unit) <= _unit.MaxRange);
        if (closestChangelingInRange != null) {
            _unit.Attack(closestChangelingInRange);
        }
    }
}
