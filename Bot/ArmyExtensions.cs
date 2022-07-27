using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;

namespace Bot;

public static class ArmyExtensions {
    public static float GetForce(this IEnumerable<Unit> army) {
        return army.Sum(soldier => soldier.FoodRequired);
    }

    public static Vector3 GetCenter(this IEnumerable<Unit> army) {
        return Clustering.GetCenter(army.ToList());
    }

    public static bool IsFighting(this IEnumerable<Unit> army) {
        return army.Any(soldier => soldier.RawUnitData.EngagedTargetTag != 0);
    }
}
