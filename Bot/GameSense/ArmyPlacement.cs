using System.Collections.Generic;
using System.Numerics;

namespace Bot.GameSense;

using Army = List<Unit>;

// TODO GD Do it
public static class ArmyPlacement {
    public static Dictionary<Unit, Vector3> ComputeConcave(Army ownArmy, Army enemyArmy, bool ignoreEnemyUnitsCollisions = false) {
        var placements = new Dictionary<Unit, Vector3>();

        // Get available surface
        // Place soldiers close to the enemy, starting with the closest ones
        // Don't get too close, just enough so that most of our army is in range

        return placements;
    }
}
