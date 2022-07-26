using System.Linq;

namespace Bot.UnitModules;

public class TargetNeutralUnitsModule: IUnitModule {
    public const string Tag = "target-neutral-units-module";

    private readonly Unit _unit;

    public static void Install(Unit unit) {
        if (!unit.Modules.ContainsKey(Tag)) {
            unit.Modules.Add(Tag, new TargetNeutralUnitsModule(unit));
        }
    }

    private TargetNeutralUnitsModule(Unit unit) {
        _unit = unit;
    }

    public void Execute() {
        var enemyUnitsInRange = Controller.EnemyUnits
            .Where(enemy => !enemy.RawUnitData.IsFlying)
            .Where(enemy => enemy.Position.HorizontalDistance(_unit.Position) <= _unit.UnitTypeData.SightRange);

        if (enemyUnitsInRange.Any()) {
            return;
        }

        var neutralUnitInRange = Controller.NeutralUnits
            .Where(neutralUnit => !neutralUnit.RawUnitData.IsFlying)
            .FirstOrDefault(neutralUnit => neutralUnit.Position.HorizontalDistance(_unit.Position) <= _unit.UnitTypeData.SightRange);

        if (neutralUnitInRange != null) {
            _unit.Attack(neutralUnitInRange);
        }
    }
}
