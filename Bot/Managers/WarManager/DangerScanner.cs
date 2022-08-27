using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.Managers;

public static class DangerScanner {
    private const float InVicinity = 12;

    // TODO GD Return a danger report
    // TODO GD Use regions?
    public static IEnumerable<Unit> GetEndangeredExpands() {
        var expands = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.TownHalls).Where(townHall => townHall.Supervisor != null);
        foreach (var expand in expands) {
            var enemyUnitsCloseBy = UnitsTracker.EnemyUnits.Where(unit => unit.HorizontalDistanceTo(expand) < InVicinity).ToList();
            if (enemyUnitsCloseBy.Count == 0) {
                continue;
            }

            var enemyUnitsCenter = enemyUnitsCloseBy.GetCenter();
            var ownedUnitsCloseBy = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Military).Where(unit => unit.HorizontalDistanceTo(expand) < InVicinity || unit.HorizontalDistanceTo(enemyUnitsCenter) < InVicinity);

            var enemyForce = enemyUnitsCloseBy.GetForce();
            var ownForce = ownedUnitsCloseBy.GetForce();

            if (enemyForce > ownForce) {
                yield return expand;
            }
        }
    }
}
