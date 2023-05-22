using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.MapAnalysis.RegionAnalysis;

namespace Bot.ExtensionMethods;

public static class ArmyExtensions {
    public static Vector2 GetCenter(this IEnumerable<Unit> army) {
        var armyList = army.ToList();
        if (armyList.Count <= 0) {
            Logger.Error("Trying to GetCenter of an empty army");

            return default;
        }

        var avgX = armyList.Average(soldier => soldier.Position.X);
        var avgY = armyList.Average(soldier => soldier.Position.Y);

        var armyCenter = new Vector2(avgX, avgY);

        return armyList
            .MinBy(soldier => soldier.Position.ToVector2().DistanceTo(armyCenter))!
            .Position
            .ToVector2()
            .AsWorldGridCenter();
    }

    /// <summary>
    /// Returns the region that is most occupied by the army.
    /// Can return null if no army is provided or if includeObstructed is false and the occupied region is obstructed.
    /// </summary>
    /// <param name="army">The army to find the region of</param>
    /// <param name="includeObstructed">Whether or not to include obstructed units</param>
    /// <returns>The region most occupied by this army or null.</returns>
    public static IRegion GetRegion(this IEnumerable<Unit> army, bool includeObstructed = false) {
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
