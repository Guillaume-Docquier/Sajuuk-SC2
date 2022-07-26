using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Bot;

public static class ArmyExtensions {
    public static float GetForce(this IEnumerable<Unit> army) {
        return army.Sum(soldier => soldier.FoodRequired);
    }

    public static Vector3 GetCenter(this IEnumerable<Unit> army) {
        return Clustering.GetCenter(army.ToList());
    }
}
