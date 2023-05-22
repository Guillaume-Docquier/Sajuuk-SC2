using System;
using System.Collections.Generic;
using System.Linq;
using Bot.GameData;
using Bot.MapAnalysis.RegionAnalysis;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.GameSense.RegionsEvaluationsTracking.RegionsEvaluations;

public class RegionsValueEvaluator : RegionsEvaluator {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IFrameClock _frameClock;
    private readonly IUnitEvaluator _unitEvaluator;

    private readonly Alliance _alliance;

    private static readonly ulong HalfLife = TimeUtils.SecsToFrames(240);
    private static readonly double ExponentialDecayConstant = Math.Log(2) / HalfLife;

    public RegionsValueEvaluator(
        IUnitsTracker unitsTracker,
        IFrameClock frameClock,
        IUnitEvaluator unitEvaluator,
        Alliance alliance
    ) : base(frameClock, "value") {
        _unitsTracker = unitsTracker;
        _frameClock = frameClock;
        _unitEvaluator = unitEvaluator;

        _alliance = alliance;
    }

    /// <summary>
    /// Evaluates the value of each region based on the units within.
    /// The value decays as the information grows older.
    /// </summary>
    protected override IEnumerable<(IRegion region, float evaluation)> DoUpdateEvaluations(IReadOnlyCollection<IRegion> regions) {
        // TODO GD Precompute units by region in a tracker
        var unitsByRegion = _unitsTracker.GetUnits(_alliance)
            .Concat(_unitsTracker.GetGhostUnits(_alliance))
            .GroupBy(unit => unit.GetRegion())
            .Where(unitsInRegion => unitsInRegion.Key != null)
            .ToDictionary(unitsInRegion => unitsInRegion.Key, unitsInRegion => unitsInRegion);

        // TODO GD Maybe we want to consider MemorizedUnits as well, but with a special treatment
        // This would avoid wierd behaviours when units jiggle near the fog of war limit
        foreach (var region in regions) {
            if (!unitsByRegion.ContainsKey(region)) {
                yield return (region, 0);
                continue;
            }

            var forceEvaluation = unitsByRegion[region].Sum(unit => _unitEvaluator.EvaluateValue(unit) * GetUnitUncertaintyPenalty(unit));

            yield return (region, forceEvaluation);
        }
    }

    /// <summary>
    /// Penalize the value of units (like ghost units) that have not been seen in a while.
    /// </summary>
    /// <param name="unit"></param>
    /// <returns>The value penalty, within ]0, 1]</returns>
    private float GetUnitUncertaintyPenalty(Unit unit) {
        // TODO GD Maybe make terran building that can fly uncertain, but not so much
        // Buildings can be considered static
        if (Units.Buildings.Contains(unit.UnitType)) {
            return 1;
        }

        // Avoid computing decay for nothing
        if (_frameClock.CurrentFrame == unit.LastSeen) {
            return 1;
        }

        return ExponentialDecayFactor(_frameClock.CurrentFrame - unit.LastSeen);
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
