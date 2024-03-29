﻿using System.Collections.Generic;
using System.Linq;
using Sajuuk.GameData;
using Sajuuk.GameSense;

namespace Sajuuk.UnitModules;

public class AttackPriorityModule: UnitModule {
    public const string ModuleTag = "AttackPriorityModule";

    private readonly IUnitsTracker _unitsTracker;

    // TODO GD Get Unit specific priorities
    private static readonly HashSet<uint> PriorityTargets = new HashSet<uint>
    {
        Units.SiegeTank,
        Units.SiegeTankSieged,
        Units.Colossus,
        Units.Immortal,
    };

    private readonly Unit _unit;

    public AttackPriorityModule(
        IUnitsTracker unitsTracker,
        Unit unit
    ) : base(ModuleTag) {
        _unitsTracker = unitsTracker;
        _unit = unit;
    }

    protected override void DoExecute() {
        if (_unit.MaxRange == 0) {
            return;
        }

        if (!_unit.IsReadyToAttack) {
            return;
        }

        if (_unit.Orders.All(order => order.AbilityId != Abilities.Attack)) {
            return;
        }

        var priorityTargetInRange = _unitsTracker.GetUnits(_unitsTracker.EnemyUnits, PriorityTargets)
            .Where(priorityTarget => priorityTarget.DistanceTo(_unit) < _unit.MaxRange)
            .MinBy(priorityTarget => priorityTarget.DistanceTo(_unit));

        if (priorityTargetInRange != null) {
            _unit.Attack(priorityTargetInRange);
        }
    }
}
