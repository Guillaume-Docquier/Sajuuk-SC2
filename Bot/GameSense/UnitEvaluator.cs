using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.GameSense;

public static class UnitEvaluator {
    public static class Force {
        public const float None = 0f;
        public const float Unknown = 1f;
        public const float Neutral = 1f;
        public const float Medium = 2f;
        public const float Strong = 5f;
        public const float Lethal = 15f;
    }

    public static class Value {
        public const float None = 0f;
        public const float Unknown = 1f;
        public const float Intriguing = 1f;
        public const float Valuable = 2f;
        public const float Prized = 5f;
        public const float Jackpot = 15f;
    }

    /// <summary>
    /// Evaluates the force of a unit
    /// TODO GD Make this more sophisticated (based on unit cost, range, counters, etc)
    /// </summary>
    /// <param name="unit">The unit to evaluate</param>
    /// <param name="areWorkersOffensive">Whether we should consider the workers as being offensive</param>
    /// <returns>The force of the unit</returns>
    public static float EvaluateForce(Unit unit, bool areWorkersOffensive = false) {
        // TODO GD Review this
        // 1 when not yet operational because buildings are tricky since their HP goes up
        // But it'll bad for warping units
        var integrityFactor = unit.IsOperational ? unit.Integrity : 1;

        return GetUnitForce(unit, areWorkersOffensive) * integrityFactor;
    }

    private static float GetUnitForce(Unit unit, bool areWorkersOffensive = false) {
        // TODO GD For now we purposefully don't handle air units, so we can't kill them
        if (unit.IsFlying) {
            return Force.None;
        }

        // TODO GD This should be more nuanced, a lone dropship is more dangerous than a dropship with a visible army
        if (Units.DropShips.Contains(unit.UnitType)) {
            return Force.Strong;
        }

        if (Units.CreepTumors.Contains(unit.UnitType)) {
            return Force.None;
        }

        if (Units.Military.Contains(unit.UnitType)) {
            if (unit.UnitType == Units.Zergling) {
                // Zerglings are very small
                return Force.Medium / 4;
            }

            return Force.Medium / 2;
        }

        if (Units.Workers.Contains(unit.UnitType)) {
            if (areWorkersOffensive || IsOffensive(unit, unit.Alliance)) {
                return Force.Medium / 4;
            }

            return Force.None;
        }

        if (Units.StaticDefenses.Contains(unit.UnitType)) {
            return Force.Medium;
        }

        if (unit.UnitType == Units.Pylon) {
            if (IsOffensive(unit, unit.Alliance)) {
                return Force.Medium;
            }

            return Force.Medium / 3;
        }

        // The rest are buildings
        // TODO GD Consider production buildings as somewhat dangerous (they produce units)
        return Force.None;
    }

    /// <summary>
    /// Evaluates the value of a unit
    /// </summary>
    /// <param name="unit">The valuable unit</param>
    /// <returns>The value of the unit</returns>
    public static float EvaluateValue(Unit unit) {
        // TODO GD For now we purposefully don't handle air units, so we can't kill them
        if (unit.IsFlying) {
            return Value.None;
        }

        if (Units.TownHalls.Contains(unit.UnitType)) {
            // TODO GD Value based on remaining resources
            if (ExpandAnalyzer.ExpandLocations.Any(expandLocation => expandLocation.Position.DistanceTo(unit) <= 1)) {
                return Value.Prized;
            }

            return Value.Prized;
        }

        if (Units.CreepTumors.Contains(unit.UnitType)) {
            return Value.Intriguing / 32;
        }

        if (Units.Workers.Contains(unit.UnitType) && !IsOffensive(unit, unit.Alliance)) {
            return Value.Intriguing;
        }

        // Losing a zerg production building prevent any unit of that type to be produced
        // It's often a very good target
        if (Units.ZergProductionBuildings.Contains(unit.UnitType)) {
            return Value.Intriguing * 3;
        }

        if (Units.ProductionBuildings.Contains(unit.UnitType)) {
            return Value.Intriguing;
        }

        if (Units.TechBuildings.Contains(unit.UnitType)) {
            return Value.Valuable;
        }

        // The rest are Military units
        return Value.Intriguing / 8;
    }

    /// <summary>
    /// Determines if this unit is offensive
    /// This is useful to determine if a pylon is a proxy pylon or if a worker is rushing
    /// An offensive unit is closer to "them" than it is to "us"
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="myAlliance"></param>
    /// <returns></returns>
    private static bool IsOffensive(Unit unit, Alliance myAlliance) {
        var myMain = ExpandAnalyzer.GetExpand(myAlliance, ExpandType.Main);
        var theirMain = ExpandAnalyzer.GetExpand(myAlliance.GetOpposing(), ExpandType.Main);

        return unit.DistanceTo(theirMain.Position) < unit.DistanceTo(myMain.Position);
    }
}
