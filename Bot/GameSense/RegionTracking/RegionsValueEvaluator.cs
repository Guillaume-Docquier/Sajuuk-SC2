using System;
using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.MapKnowledge;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.GameSense.RegionTracking;

public class RegionsValueEvaluator : IRegionsEvaluator {
    private readonly Alliance _alliance;
    private readonly bool _hasAbsoluteKnowledge;
    private Dictionary<Region, float> _regionValues;
    private Dictionary<Region, float> _normalizedRegionValues;

    // It would be cool to use the exponential decay everywhere
    // But that would require tracking the last value update of each region
    // And we would be doing some fancy (expensive?) exponent operations
    // But in the end, a good ol' factor works perfectly well, so... maybe some other day
    private static readonly float RegionDecayRate = 1f - 1f / TimeUtils.SecsToFrames(30);

    private static readonly ulong HalfLife = TimeUtils.SecsToFrames(120);
    private static readonly double ExponentialDecayConstant = Math.Log(2) / HalfLife;

    public RegionsValueEvaluator(Alliance alliance) {
        _alliance = alliance;
        _hasAbsoluteKnowledge = alliance == Alliance.Self;
    }

    /// <summary>
    /// Gets the evaluated value of the provided region
    /// </summary>
    /// <param name="region">The region to get the evaluated value of</param>
    /// <param name="normalized">Whether or not to get the normalized value between 0 and 1.</param>
    /// <returns>The evaluated value of the region</returns>
    public float GetEvaluation(Region region, bool normalized = false) {
        if (region == null || !_regionValues.ContainsKey(region)) {
            Logger.Error("Trying to get the value of an unknown region: {0}. {1} regions are known.", region, _regionValues.Count);
            return UnitEvaluator.Value.Unknown;
        }

        if (normalized) {
            return _normalizedRegionValues[region];
        }

        return _regionValues[region];
    }

    /// <summary>
    /// Initializes the region values of the provided regions
    /// </summary>
    public void Init(List<Region> regions) {
        var initialValue = _hasAbsoluteKnowledge
            ? UnitEvaluator.Value.None
            : UnitEvaluator.Value.Unknown;

        _regionValues = new Dictionary<Region, float>();
        foreach (var region in regions) {
            _regionValues[region] = initialValue;
        }
    }

    /// <summary>
    /// Evaluates the value of each region based on the units and fog of war
    /// The value decays as the information grows older
    /// </summary>
    public void Evaluate() {
        // Update based on units
        var newRegionValues = ComputeUnitsValue();

        // Update based on visibility
        foreach (var (region, fogOfWarDanger) in ComputeFogOfWarValue()) {
            if (!newRegionValues.ContainsKey(region)) {
                newRegionValues[region] = fogOfWarDanger;
            }
            else {
                newRegionValues[region] += fogOfWarDanger;
            }
        }

        if (!_hasAbsoluteKnowledge) {
            var enemySpawnValueCue = GetSpawnValueCue();
            if (!newRegionValues.ContainsKey(enemySpawnValueCue.Region)) {
                newRegionValues[enemySpawnValueCue.Region] = enemySpawnValueCue.Value;
            }
            else {
                newRegionValues[enemySpawnValueCue.Region] = Math.Max(enemySpawnValueCue.Value, newRegionValues[enemySpawnValueCue.Region]);
            }
        }

        // Update the values
        foreach (var (region, newRegionValue) in newRegionValues) {
            // Let high value decay over time
            if (newRegionValue > UnitEvaluator.Value.Intriguing) {
                _regionValues[region] = Math.Max(_regionValues[region], newRegionValue);
            }
            // Let low value increase over time
            else {
                _regionValues[region] = Math.Min(_regionValues[region], newRegionValue);
            }
        }

        if (!_hasAbsoluteKnowledge) {
            DecayValues();
        }

        _normalizedRegionValues = ComputeNormalizedValues(_regionValues);
    }

    /// <summary>
    /// For each region, compute the value of the units within
    /// </summary>
    /// <returns>A value score for each region</returns>
    private Dictionary<Region, float> ComputeUnitsValue() {
        var regionValues = new Dictionary<Region, float>();
        var unitsToConsider = UnitsTracker.GetUnits(_alliance).Concat(UnitsTracker.GetGhostUnits(_alliance));
        foreach (var unit in unitsToConsider) {
            var region = unit.Position.GetRegion();
            if (region == null) {
                continue;
            }

            var value = UnitEvaluator.EvaluateValue(unit) * GetUnitUncertaintyPenalty(unit);
            if (!regionValues.ContainsKey(region)) {
                regionValues[region] = value;
            }
            else {
                regionValues[region] += value;
            }
        }

        return regionValues;
    }

    /// <summary>
    /// Penalize the value of units (like ghost units) that have not been seen in a while.
    /// </summary>
    /// <param name="enemy"></param>
    /// <returns>The value penalty, within ]0, 1]</returns>
    private static float GetUnitUncertaintyPenalty(Unit enemy) {
        // TODO GD Maybe make terran building that can fly uncertain, but not so much
        // Buildings can be considered static
        if (Units.Buildings.Contains(enemy.UnitType)) {
            return 1;
        }

        // Avoid computing decay for nothing
        if (Controller.Frame == enemy.LastSeen) {
            return 1;
        }

        return ExponentialDecayFactor(Controller.Frame - enemy.LastSeen);
    }

    /// <summary>
    /// For each region, return some value based on the non-visible portion of the region.
    /// </summary>
    /// <returns>The value of each region based on the visible percentage of the region</returns>
    private Dictionary<Region, float> ComputeFogOfWarValue() {
        if (_hasAbsoluteKnowledge) {
            return new Dictionary<Region, float>();
        }

        // TODO GD This should be cached in the VisibilityTracker, probably
        var regionVisibility = new Dictionary<Region, int>();
        foreach (var visibleCell in VisibilityTracker.VisibleCells) {
            var region = visibleCell.GetRegion();
            if (region == null) {
                continue;
            }

            if (!regionVisibility.ContainsKey(region)) {
                regionVisibility[region] = 1;
            }
            else {
                regionVisibility[region] += 1;
            }
        }

        var fogOfWarValue = new Dictionary<Region, float>();
        foreach (var (region, visibleCellCount) in regionVisibility) {
            var percentNotVisible = 1 - (float)visibleCellCount / region.Cells.Count;
            fogOfWarValue[region] = percentNotVisible * UnitEvaluator.Value.Unknown;
        }

        return fogOfWarValue;
    }

    /// <summary>
    /// Returns the exponential decay factor of a value after some time
    /// </summary>
    /// <param name="time">The time since decay started</param>
    /// <returns>The decay factor</returns>
    private static float ExponentialDecayFactor(ulong time) {
        return (float)Math.Exp(-ExponentialDecayConstant * time);
    }

    /// <summary>
    /// Gets a value cue for the spawn.
    /// This is because we know where the enemy is, but the bot doesn't see it.
    /// </summary>
    /// <returns></returns>
    private (Region Region, float Value) GetSpawnValueCue() {
        var spawnRegion = ExpandAnalyzer.GetExpand(_alliance, ExpandType.Main).Position.GetRegion();
        var spawnRegionExplorationPercentage = (float)spawnRegion.Cells.Count(VisibilityTracker.IsExplored) / spawnRegion.Cells.Count;

        // No cue if we've explored it
        if (spawnRegionExplorationPercentage > 0.6) {
            return (spawnRegion, 0);
        }

        var value = UnitEvaluator.Value.Jackpot * (1 - spawnRegionExplorationPercentage);
        return (spawnRegion, value);
    }

    /// <summary>
    /// Decays the values to represent uncertainty over time
    /// </summary>
    private void DecayValues() {
        // Decay towards Intriguing over time
        foreach (var region in _regionValues.Keys) {
            var normalizedTowardsIntriguingValue = _regionValues[region] - UnitEvaluator.Value.Intriguing;
            var decayedValue = normalizedTowardsIntriguingValue * RegionDecayRate + UnitEvaluator.Value.Intriguing;
            if (Math.Abs(UnitEvaluator.Value.Intriguing - decayedValue) < 0.05) {
                decayedValue = UnitEvaluator.Value.Intriguing;
            }

            _regionValues[region] = decayedValue;
        }
    }

    /// <summary>
    /// Normalizes the given values to put them between 0 and 1.
    /// </summary>
    /// <param name="values">The values to normalize</param>
    /// <returns>A new dictionary with the values normalized</returns>
    private static Dictionary<Region, float> ComputeNormalizedValues(Dictionary<Region, float> values) {
        var minValue = values.Values.Min();
        var maxValue = values.Values.Max();

        var normalizedValues = new Dictionary<Region, float>();
        foreach (var region in values.Keys) {
            normalizedValues[region] = MathUtils.Normalize(values[region], minValue, maxValue);
        }

        return normalizedValues;
    }
}
