using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement;

public static class DangerScanner {
    public static bool IsRushInProgress(List<Unit> ownArmy) {
        var main = ExpandAnalyzer.GetExpand(Alliance.Ally, ExpandType.Main).Position.GetRegion();
        var natural = ExpandAnalyzer.GetExpand(Alliance.Ally, ExpandType.Natural).Position.GetRegion();

        var regionsToProtect = Pathfinder.FindPath(main, natural);

        // TODO GD Per region basis or just globally?
        foreach (var regionToProtect in regionsToProtect) {
            var enemyForce = UnitsTracker.EnemyUnits
                .Where(soldier => soldier.GetRegion() == regionToProtect)
                .Sum(UnitEvaluator.EvaluateForce);

            var ownForce = ownArmy
                .Where(soldier => soldier.GetRegion() == regionToProtect)
                .Sum(UnitEvaluator.EvaluateForce);

            if (enemyForce > ownForce) {
                return true;
            }
        }

        return false;
    }
}
