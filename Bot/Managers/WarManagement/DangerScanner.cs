using System.Collections.Generic;
using System.Linq;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement;

public static class DangerScanner {
    // TODO GD Return a danger report containing enemy units
    // TODO GD Use regions?
    public static IEnumerable<Unit> GetEndangeredExpands() {
        var expands = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.TownHalls).Where(townHall => townHall.Supervisor != null);
        foreach (var expand in expands) {
            var enemyForce = RegionTracker.GetForce(expand.GetRegion(), Alliance.Enemy);
            var ownForce = RegionTracker.GetForce(expand.GetRegion(), Alliance.Self);

            if (enemyForce > ownForce) {
                yield return expand;
            }
        }
    }
}
