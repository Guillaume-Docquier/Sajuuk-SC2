using System.Linq;
using Bot.GameData;

namespace Bot.UnitModules;

public class AttackPriorityModule: IUnitModule {
    public const string Tag = "attack-priority-module";

    private readonly Unit _unit;

    public static void Install(Unit unit) {
        if (!unit.Modules.ContainsKey(Tag)) {
            unit.Modules.Add(Tag, new AttackPriorityModule(unit));
        }
    }

    private AttackPriorityModule(Unit unit) {
        _unit = unit;
    }

    public void Execute() {
        if (_unit.Orders.All(order => order.AbilityId != Abilities.Attack)) {
            return;
        }

        var unitWeapon = _unit.UnitTypeData.Weapons.MaxBy(weapon => weapon.Range);
        if (unitWeapon == null) {
            return;
        }

        // TODO GD Add other units to prioritize
        var priorityTargetInRange = Controller.GetUnits(Controller.EnemyUnits, Units.SiegeTanks)
            .Where(priorityTarget => priorityTarget.DistanceTo(_unit) < unitWeapon.Range)
            .MinBy(priorityTarget => priorityTarget.DistanceTo(_unit));

        if (priorityTargetInRange == null) {
            return;
        }

        _unit.Attack(priorityTargetInRange);
    }
}
