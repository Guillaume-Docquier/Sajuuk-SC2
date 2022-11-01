using System.Collections.Generic;
using System.Linq;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.UnitModules;

public class AttackPriorityModule: UnitModule {
    public const string Tag = "AttackPriorityModule";

    // TODO GD Get Unit specific priorities
    private static readonly HashSet<uint> PriorityTargets = new HashSet<uint>
    {
        Units.SiegeTank,
        Units.SiegeTankSieged,
        Units.Colossus,
        Units.Immortal,
    };

    private readonly Unit _unit;

    public static void Install(Unit unit) {
        if (PreInstallCheck(Tag, unit)) {
            unit.Modules.Add(Tag, new AttackPriorityModule(unit));
        }
    }

    private AttackPriorityModule(Unit unit) {
        _unit = unit;
    }

    protected override void DoExecute() {
        if (_unit.MaxRange == 0) {
            return;
        }

        if (_unit.Orders.All(order => order.AbilityId != Abilities.Attack)) {
            return;
        }

        var priorityTargetInRange = Controller.GetUnits(UnitsTracker.EnemyUnits, PriorityTargets)
            .Where(priorityTarget => priorityTarget.DistanceTo(_unit) < _unit.MaxRange)
            .MinBy(priorityTarget => priorityTarget.DistanceTo(_unit));

        if (priorityTargetInRange != null) {
            _unit.Attack(priorityTargetInRange);
        }
    }
}
