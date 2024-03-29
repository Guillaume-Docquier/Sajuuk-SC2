﻿using System.Collections.Generic;
using System.Linq;
using Sajuuk.ExtensionMethods;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameData;
using Sajuuk.GameSense;

namespace Sajuuk.Managers.WarManagement.ArmySupervision.UnitsControl;

/// <summary>
/// Give attack commands on their current position to units that are ready to
/// attack, have a move order and have enemies in range.
/// </summary>
public class DisengagementKiting : IUnitsControl {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;

    private const bool Debug = true;

    public DisengagementKiting(IUnitsTracker unitsTracker, IGraphicalDebugger graphicalDebugger) {
        _unitsTracker = unitsTracker;
        _graphicalDebugger = graphicalDebugger;
    }

    public bool IsExecuting() {
        return false;
    }

    public IReadOnlySet<Unit> Execute(IReadOnlySet<Unit> army) {
        var unitThatHaveWeapons = army.Where(unit => unit.HasWeapons).ToList();
        if (!unitThatHaveWeapons.Any()) {
            return army;
        }

        var uncontrolledUnits = new HashSet<Unit>(army);

        foreach (var unit in unitThatHaveWeapons) {
            if (!unit.IsReadyToAttack) {
                continue;
            }

            var hasKiteOrders = unit.Orders
                .Where(order => order.AbilityId == Abilities.Attack)
                .Where(order => order.TargetWorldSpacePos != null)
                .Any(order => order.TargetWorldSpacePos.ToVector2().DistanceTo(unit) <= 0.5);

            if (hasKiteOrders && unit.IsFightingTheEnemy) {
                // Finish your attack
                DebugUnitKite(unit);
                uncontrolledUnits.Remove(unit);
                continue;
            }

            if (unit.Orders.Any(order => order.AbilityId != Abilities.Move)) {
                continue;
            }

            var enemiesInRange = _unitsTracker.EnemyUnits
                .Where(enemy => unit.CanAttack(enemy))
                .Where(enemy => unit.IsInAttackRangeOf(enemy));

            if (!enemiesInRange.Any()) {
                continue;
            }

            unit.AttackMove(unit.Position.ToVector2());
            DebugUnitKite(unit);
            uncontrolledUnits.Remove(unit);
        }

        return uncontrolledUnits;
    }

    public void Reset(IReadOnlyCollection<Unit> army) {
        // Nothing to do
    }

    private void DebugUnitKite(Unit unit) {
        if (!Debug) {
            return;
        }

        _graphicalDebugger.AddText("KITE", worldPos: unit.Position.ToPoint(yOffset: 0.51f));
    }
}
