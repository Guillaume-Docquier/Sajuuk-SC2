using System.Linq;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.UnitModules;

public class TargetNeutralUnitsModule: UnitModule {
    public const string Tag = "TargetNeutralUnitsModule";

    private readonly Unit _unit;

    public static void Install(Unit unit) {
        if (PreInstallCheck(Tag, unit)) {
            unit.Modules.Add(Tag, new TargetNeutralUnitsModule(unit));
        }
    }

    private TargetNeutralUnitsModule(Unit unit) {
        _unit = unit;
    }

    protected override void DoExecute() {
        var enemyUnitsInRange = UnitsTracker.EnemyUnits
            .Where(enemy => !enemy.RawUnitData.IsFlying)
            .Where(enemy => enemy.HorizontalDistanceTo(_unit.Position) <= _unit.UnitTypeData.SightRange);

        if (enemyUnitsInRange.Any()) {
            return;
        }

        var neutralUnitInRange = Controller.GetUnits(UnitsTracker.NeutralUnits, Units.Destructibles)
            .Where(neutralUnit => !neutralUnit.RawUnitData.IsFlying)
            .FirstOrDefault(neutralUnit => neutralUnit.HorizontalDistanceTo(_unit.Position) <= _unit.UnitTypeData.SightRange);

        if (neutralUnitInRange != null) {
            _unit.Attack(neutralUnitInRange);
        }
    }
}
