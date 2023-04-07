using System;
using System.Collections.Generic;
using System.Linq;
using Bot.GameData;
using Bot.MapKnowledge;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.GameSense.RegionTracking;

public class RegionsValueEvaluator : RegionsEvaluator {
    private static readonly ulong HalfLife = TimeUtils.SecsToFrames(120);
    private static readonly double ExponentialDecayConstant = Math.Log(2) / HalfLife;

    public RegionsValueEvaluator(Alliance alliance) : base(alliance, "value") {}

    /// <summary>
    /// Evaluates the value of each region based on the units within.
    /// The value decays as the information grows older.
    /// </summary>
    protected override IEnumerable<(Region region, float value)> DoEvaluate(IReadOnlyCollection<Region> regions) {
        // TODO GD Maybe we want to consider MemorizedUnits as well, but with a special treatment
        // This would avoid wierd behaviours when units jiggle near the fog of war limit
        return UnitsTracker.GetUnits(Alliance)
            .Concat(UnitsTracker.GetGhostUnits(Alliance))
            // TODO GD Precompute units by region in a tracker
            .GroupBy(unit => unit.GetRegion())
            // We might be evaluating regions outside of the provided regions
            // In reality, regions is all the regions, so it doesn't matter.
            .Where(unitsInRegion => unitsInRegion.Key != null)
            .Select(unitsInRegion => {
                var regionValue = unitsInRegion.Sum(unit => UnitEvaluator.EvaluateValue(unit) * GetUnitUncertaintyPenalty(unit));
                return (unitsInRegion.Key, regionValue);
            });
    }

    /// <summary>
    /// Penalize the value of units (like ghost units) that have not been seen in a while.
    /// </summary>
    /// <param name="unit"></param>
    /// <returns>The value penalty, within ]0, 1]</returns>
    private static float GetUnitUncertaintyPenalty(Unit unit) {
        // TODO GD Maybe make terran building that can fly uncertain, but not so much
        // Buildings can be considered static
        if (Units.Buildings.Contains(unit.UnitType)) {
            return 1;
        }

        // Avoid computing decay for nothing
        if (Controller.Frame == unit.LastSeen) {
            return 1;
        }

        return ExponentialDecayFactor(Controller.Frame - unit.LastSeen);
    }

    /// <summary>
    /// Returns the exponential decay factor of a value after some time
    /// </summary>
    /// <param name="age">The time since decay started</param>
    /// <returns>The decay factor</returns>
    private static float ExponentialDecayFactor(ulong age) {
        return (float)Math.Exp(-ExponentialDecayConstant * age);
    }
}
