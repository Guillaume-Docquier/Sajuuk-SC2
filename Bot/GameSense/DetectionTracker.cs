using System.Linq;
using Bot.GameData;

namespace Bot.GameSense;

public static class DetectionTracker {
    private static bool _enemyHasDetectors = false;

    public static bool IsStealthEffective() {
        if (_enemyHasDetectors) {
            return false;
        }

        // We can't kill flying units for now, so we can cache this value
        _enemyHasDetectors = Controller.GetUnits(Controller.EnemyUnits, Units.MobileDetectors).Any();

        return !_enemyHasDetectors;
    }
}
