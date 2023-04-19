using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

/// <summary>
/// Give attack commands on their current position to units that are ready to
/// attack, have a move order and have enemies in range.
/// </summary>
public class DisengagementKiting : IUnitsControl {
    private const bool Debug = true;

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

            var enemiesInRange = UnitsTracker.EnemyUnits
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

    private static void DebugUnitKite(Unit unit) {
        if (!Debug) {
            return;
        }

        Program.GraphicalDebugger.AddText("KITE", worldPos: unit.Position.ToPoint(yOffset: 0.51f));
    }
}
