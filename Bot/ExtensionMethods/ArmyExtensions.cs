using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameSense;
using Bot.MapKnowledge;

namespace Bot.ExtensionMethods;

public static class ArmyExtensions {
    public static float GetForce(this IEnumerable<Unit> army, bool areWorkersOffensive = false) {
        return army.Sum(soldier => UnitEvaluator.EvaluateForce(soldier, areWorkersOffensive));
    }

    public static Vector2 GetCenter(this IEnumerable<Unit> army) {
        var armyList = army.ToList();
        var armyCenter = Clustering.GetCenter(armyList);

        return armyList
            .MinBy(soldier => soldier.Position.ToVector2().DistanceTo(armyCenter))!
            .Position
            .ToVector2()
            .AsWorldGridCenter()
            .ClosestWalkable(searchRadius: 3);
    }

    /// <summary>
    /// Returns the region that is most occupied by the army.
    /// Can return null if no army is provided or if includeObstructed is false and the occupied region is obstructed.
    /// </summary>
    /// <param name="army">The army to find the region of</param>
    /// <param name="includeObstructed">Whether or not to include obstructed units</param>
    /// <returns>The region most occupied by this army or null.</returns>
    public static Region GetRegion(this IEnumerable<Unit> army, bool includeObstructed = false) {
        var potentialRegions = army
            .Select(soldier => soldier.GetRegion())
            .Where(region => region != null)
            .Where(region => includeObstructed || !region.IsObstructed)
            .ToList();

        if (potentialRegions.Count == 0) {
            Logger.Warning("Cannot get a region for this army");
            return null;
        }

        return potentialRegions
            .GroupBy(region => region)
            .MaxBy(group => group.Count())!
            .Key;
    }

    public static bool IsFighting(this IEnumerable<Unit> army) {
        return army.Any(soldier => soldier.RawUnitData.EngagedTargetTag != 0);
    }
}
