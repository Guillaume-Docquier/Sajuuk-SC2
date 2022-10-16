using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.GameSense;

public class RegionTracker : INeedUpdating {
    public static RegionTracker Instance { get; private set; } = new RegionTracker();

    private bool _isInitialized = false;
    private Dictionary<Region, float> _regionDangerLevels;

    private static readonly float DecayRate = 1f - 1f / Controller.SecsToFrames(10);

    private static class DangerLevel {
        public const float Safe = 0f;
        public const float Unknown = 1f;
        public const float Neutral = 1f;
        public const float Dangerous = 2f;
        public const float VeryDangerous = 5f;
        public const float Lethal = 15f;
    }

    private static readonly List<Color> RegionColors = new List<Color>
    {
        Colors.MulberryRed,
        Colors.MediumTurquoise,
        Colors.SunbrightOrange,
        Colors.PeachPink,
        Colors.Purple,
        Colors.LimeGreen,
        Colors.BurlywoodBeige,
        Colors.LightRed,
    };

    private RegionTracker() {}

    public void Reset() {
        Instance = new RegionTracker();
    }

    public void Update(ResponseObservation observation) {
        if (!RegionAnalyzer.IsInitialized) {
            return;
        }

        if (!Instance._isInitialized) {
            Instance.Init();
        }

        Instance.UpdateDangerLevels();
        Instance.DrawRegionsSummary();
    }

    public static float GetDangerLevel(Vector3 position) {
        return GetDangerLevel(position.GetRegion());
    }

    public static float GetDangerLevel(Region region) {
        if (region == null || !Instance._regionDangerLevels.ContainsKey(region)) {
            return (int)DangerLevel.Unknown;
        }

        return Instance._regionDangerLevels[region];
    }

    private void Init() {
        _regionDangerLevels = new Dictionary<Region, float>();
        foreach (var region in RegionAnalyzer.Regions) {
            _regionDangerLevels[region] = DangerLevel.Unknown;
        }

        _isInitialized = true;
    }

    private void UpdateDangerLevels() {
        // Update based on enemy units
        var newDangerLevels = ComputeUnitsDanger();

        // Update based on visibility
        foreach (var (region, fogOfWarDanger) in ComputeFogOfWarDanger()) {
            if (!newDangerLevels.ContainsKey(region)) {
                newDangerLevels[region] = fogOfWarDanger;
            }
            else {
                newDangerLevels[region] += fogOfWarDanger;
            }
        }

        // Update the enemy spawn
        var enemySpawnRegion = MapAnalyzer.EnemyStartingLocation.GetRegion();
        var enemySpawnRegionExplorationPercentage = (float)VisibilityTracker.ExploredCells.Count(exploredCell => enemySpawnRegion.Cells.Contains(exploredCell.ToVector3())) / enemySpawnRegion.Cells.Count;
        if (enemySpawnRegionExplorationPercentage < 0.6) {
            var dangerLevel = DangerLevel.Lethal * (1 - enemySpawnRegionExplorationPercentage);
            if (!newDangerLevels.ContainsKey(enemySpawnRegion)) {
                newDangerLevels[enemySpawnRegion] = dangerLevel;
            }
            else {
                newDangerLevels[enemySpawnRegion] = Math.Max(dangerLevel, newDangerLevels[enemySpawnRegion]);
            }
        }

        // Update the danger levels
        foreach (var (region, newDangerLevel) in newDangerLevels) {
            // Let dangerous danger decay over time
            if (newDangerLevel > DangerLevel.Neutral) {
                _regionDangerLevels[region] = Math.Max(_regionDangerLevels[region], newDangerLevel);
            }
            // Let safe danger decay over time
            else {
                _regionDangerLevels[region] = Math.Min(_regionDangerLevels[region], newDangerLevel);
            }
        }

        // Decay towards neutral over time
        foreach (var region in _regionDangerLevels.Keys) {
            var normalizedTowardsNeutralDangerLevel = _regionDangerLevels[region] - DangerLevel.Neutral;
            var decayedDanger = normalizedTowardsNeutralDangerLevel * DecayRate + DangerLevel.Neutral;
            if (Math.Abs(DangerLevel.Neutral - decayedDanger) < 0.05) {
                decayedDanger = DangerLevel.Neutral;
            }

            _regionDangerLevels[region] = decayedDanger;
        }
    }

    // TODO GD Implement ghost (last seen) units to have lingering danger
    private static Dictionary<Region, float> ComputeUnitsDanger() {
        var unitsDanger = new Dictionary<Region, float>();
        foreach (var enemyUnit in UnitsTracker.EnemyUnits) {
            var region = enemyUnit.Position.GetRegion();
            if (region == null) {
                continue;
            }

            var danger = GetUnitDanger(enemyUnit);

            if (!unitsDanger.ContainsKey(region)) {
                unitsDanger[region] = danger;
            }
            else {
                unitsDanger[region] += danger;
            }
        }

        return unitsDanger;
    }

    // TODO GD Make this more sophisticated (based on unit cost, probably)
    private static float GetUnitDanger(Unit unit) {
        // TODO GD This should be more nuanced, a lone dropship is more dangerous than a dropship with a visible army
        if (Units.DropShips.Contains(unit.UnitType)) {
            return DangerLevel.VeryDangerous;
        }

        if (Units.CreepTumors.Contains(unit.UnitType)) {
            return DangerLevel.Dangerous / 16;
        }

        if (Units.Military.Contains(unit.UnitType)) {
            return DangerLevel.Dangerous / 2;
        }

        if (Units.Workers.Contains(unit.UnitType)) {
            return DangerLevel.Dangerous / 8;
        }

        if (Units.StaticDefenses.Contains(unit.UnitType)) {
            return DangerLevel.Dangerous;
        }

        if (unit.UnitType == Units.Pylon) {
            return DangerLevel.Dangerous;
        }

        // The rest are buildings
        return DangerLevel.Dangerous / 2;
    }

    private Dictionary<Region, float> ComputeFogOfWarDanger() {
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

        var fogOfWarDanger = new Dictionary<Region, float>();
        foreach (var (region, visibleCellCount) in regionVisibility) {
            var percentNotVisible = 1 - (float)visibleCellCount / region.Cells.Count;
            fogOfWarDanger[region] = percentNotVisible * DangerLevel.Unknown;
        }

        return fogOfWarDanger;
    }

    /// <summary>
    /// <para>Draws a marker over each region and links with neighbors.</para>
    /// <para>The marker also indicates the danger level of the region.</para>
    /// <para>Each region gets a different color using the color pool.</para>
    /// </summary>
    private void DrawRegionsSummary() {
        const int zOffset = 5;

        var regionIndex = 0;
        foreach (var region in RegionAnalyzer.Regions) {
            var regionColor = RegionColors[regionIndex % RegionColors.Count];
            var offsetRegionCenter = region.Center.Translate(zTranslation: zOffset);

            Program.GraphicalDebugger.AddLink(region.Center, offsetRegionCenter, color: regionColor, withText: false);
            Program.GraphicalDebugger.AddTextGroup(
                new[]
                {
                    $"R{regionIndex} ({region.Type})",
                    $"{GetDangerLevelLabel(region)} ({_regionDangerLevels[region]:F2})",
                },
                size: 14, worldPos: offsetRegionCenter.ToPoint(), color: regionColor);

            foreach (var neighbor in region.Neighbors) {
                var neighborOffsetCenter = neighbor.Region.Center.Translate(zTranslation: zOffset);
                var regionSizeRatio = (float)region.Cells.Count / (region.Cells.Count + neighbor.Region.Cells.Count); // TODO GD Link to frontier instead
                var lineEnd = Vector3.Lerp(offsetRegionCenter, neighborOffsetCenter, regionSizeRatio);
                Program.GraphicalDebugger.AddLine(offsetRegionCenter, lineEnd, color: regionColor);
            }

            regionIndex++;
        }
    }

    private string GetDangerLevelLabel(Region region) {
        var dangerLevel = _regionDangerLevels[region];

        return dangerLevel switch
        {
            >= DangerLevel.Lethal => "Lethal",
            >= DangerLevel.VeryDangerous => "VeryDangerous",
            >= DangerLevel.Dangerous => "Dangerous",
            >= DangerLevel.Neutral => "Neutral",
            _ => "Safe"
        };
    }
}
