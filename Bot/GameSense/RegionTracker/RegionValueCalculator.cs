﻿using System;
using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.MapKnowledge;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.GameSense;

public class RegionValueCalculator {
    private readonly Alliance _alliance;
    private readonly bool _hasAbsoluteKnowledge;
    private Dictionary<Region, float> _regionValues;

    // It would be cool to use the exponential decay everywhere
    // But that would require tracking the last value update of each region
    // And we would be doing some fancy (expensive?) exponent operations
    // But in the end, a good ol' factor works perfectly well, so... maybe some other day
    private static readonly float RegionDecayRate = 1f - 1f / TimeUtils.SecsToFrames(30);

    private static readonly ulong HalfLife = TimeUtils.SecsToFrames(120);
    private static readonly double ExponentialDecayConstant = Math.Log(2) / HalfLife;

    public RegionValueCalculator(Alliance alliance) {
        _alliance = alliance;
        _hasAbsoluteKnowledge = alliance == Alliance.Self;
    }

    public float GetValue(Region region) {
        if (!_regionValues.ContainsKey(region)) {
            Logger.Error("Trying to get the value of an unknown region. {0} regions are known.", _regionValues.Count);
            return RegionTracker.Value.Unknown;
        }

        return _regionValues[region];
    }

    /// <summary>
    /// Initializes the region values of the provided regions
    /// </summary>
    public void Init(List<Region> regions) {
        var initialValue = _hasAbsoluteKnowledge
            ? RegionTracker.Value.None
            : RegionTracker.Value.Unknown;

        _regionValues = new Dictionary<Region, float>();
        foreach (var region in regions) {
            _regionValues[region] = initialValue;
        }
    }

    /// <summary>
    /// Calculate the value of each region based on the units and fog of war
    /// The value decays as the information grows older
    /// </summary>
    public void Calculate() {
        // Update based on units
        var newRegionValues = ComputeUnitsValue();

        // Update based on visibility
        foreach (var (region, fogOfWarDanger) in ComputeFogOfWarDanger()) {
            if (!newRegionValues.ContainsKey(region)) {
                newRegionValues[region] = fogOfWarDanger;
            }
            else {
                newRegionValues[region] += fogOfWarDanger;
            }
        }

        // Update the values
        foreach (var (region, newRegionValue) in newRegionValues) {
            // Let high value decay over time
            if (newRegionValue > RegionTracker.Value.Intriguing) {
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

            var value = GetUnitValue(unit) * GetUnitUncertaintyPenalty(unit);
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
    /// Get the value of a unit
    /// </summary>
    /// <param name="unit">The valuable unit</param>
    /// <returns>The value of the unit</returns>
    private float GetUnitValue(Unit unit) {
        if (Units.TownHalls.Contains(unit.UnitType)) {
            // TODO GD Value based on remaining resources
            if (ExpandAnalyzer.ExpandLocations.Any(expandLocation => expandLocation.Position.DistanceTo(unit) <= 1)) {
                return RegionTracker.Value.Valuable;
            }
        }

        if (Units.Workers.Contains(unit.UnitType)) {
            return RegionTracker.Value.Intriguing / 2;
        }

        // TODO GD Handle tech buildings
        // TODO GD Handle production buildings
        // TODO GD Handle supply buildings

        return RegionTracker.Value.None;
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
    private Dictionary<Region, float> ComputeFogOfWarDanger() {
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
            fogOfWarValue[region] = percentNotVisible * RegionTracker.Value.Unknown;
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
    /// Determines if this pylon of ours is a proxy pylon
    /// My proxy pylon is closer to them than it is to me
    /// </summary>
    /// <param name="pylon"></param>
    /// <param name="myAlliance"></param>
    /// <returns></returns>
    private static bool IsProxyPylon(Unit pylon, Alliance myAlliance) {
        var mains = ExpandAnalyzer.ExpandLocations.Where(expand => expand.ExpandType == ExpandType.Main);
        var myMain = ExpandAnalyzer.GetExpand(myAlliance, ExpandType.Main);
        var theirMain = mains.First(main => main != myMain);

        return pylon.DistanceTo(theirMain.Position) < pylon.DistanceTo(myMain.Position);
    }

    /// <summary>
    /// Decays the values to represent uncertainty over time
    /// </summary>
    private void DecayValues() {
        // Decay towards Intriguing over time
        foreach (var region in _regionValues.Keys) {
            var normalizedTowardsIntriguingValue = _regionValues[region] - RegionTracker.Value.Intriguing;
            var decayedValue = normalizedTowardsIntriguingValue * RegionDecayRate + RegionTracker.Value.Intriguing;
            if (Math.Abs(RegionTracker.Value.Intriguing - decayedValue) < 0.05) {
                decayedValue = RegionTracker.Value.Intriguing;
            }

            _regionValues[region] = decayedValue;
        }
    }
}
