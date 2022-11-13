using System.Collections.Generic;
using System.Numerics;
using Bot.Debugging;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.GameSense;

public class RegionTracker : INeedUpdating {
    public static RegionTracker Instance { get; private set; } = new RegionTracker();

    private bool _isInitialized = false;

    private readonly Dictionary<Alliance, RegionForceCalculator> _regionForceCalculators = new Dictionary<Alliance, RegionForceCalculator>
    {
        { Alliance.Self, new RegionForceCalculator(Alliance.Self) },
        { Alliance.Enemy, new RegionForceCalculator(Alliance.Enemy) },
    };

    public static class ForceLevel {
        public const float None = 0f;
        public const float Unknown = 1f;
        public const float Neutral = 1f;
        public const float Medium = 2f;
        public const float Strong = 5f;
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

    /// <summary>
    /// Gets the force associated with the region of a given position
    /// </summary>
    /// <param name="position">The position to get the force of</param>
    /// <param name="alliance">The alliance to get the force of</param>
    /// <returns>The force of the position's region</returns>
    public static float GetForce(Vector2 position, Alliance alliance) {
        return GetForce(position.GetRegion(), alliance);
    }

    /// <summary>
    /// Gets the force of a region
    /// </summary>
    /// <param name="region">The region to get the force of</param>
    /// <param name="alliance">The alliance to get the force of</param>
    /// <returns>The force of the region</returns>
    public static float GetForce(Region region, Alliance alliance) {
        if (!Instance._regionForceCalculators.ContainsKey(alliance)) {
            Logger.Error("Cannot get force for alliance {0}. We don't track that", alliance);
        }

        return Instance._regionForceCalculators[alliance].GetForce(region);
    }

    public void Reset() {
        Instance = new RegionTracker();
    }

    public void Update(ResponseObservation observation) {
        if (!RegionAnalyzer.IsInitialized) {
            return;
        }

        if (!_isInitialized) {
            InitCalculators();
        }

        UpdateCalculations();

        DrawRegionsSummary();
    }

    private void InitCalculators() {
        foreach (var regionForceCalculator in _regionForceCalculators.Values) {
            regionForceCalculator.Init(RegionAnalyzer.Regions);
        }

        _isInitialized = true;
    }

    private void UpdateCalculations() {
        foreach (var regionForceCalculator in _regionForceCalculators.Values) {
            regionForceCalculator.Calculate();
        }
    }

    /// <summary>
    /// <para>Draws a marker over each region and links with neighbors.</para>
    /// <para>The marker also indicates the force of each alliance in the region.</para>
    /// <para>Each region gets a different color using the color pool.</para>
    /// </summary>
    private void DrawRegionsSummary() {
        if (!DebuggingFlagsTracker.ActiveDebuggingFlags.Contains(DebuggingFlags.Regions)) {
            return;
        }

        const int zOffset = 5;

        var regionIndex = 0;
        foreach (var region in RegionAnalyzer.Regions) {
            var regionColor = RegionColors[regionIndex % RegionColors.Count];
            var offsetRegionCenter = region.Center.ToVector3(zOffset: zOffset);

            var regionTypeText = region.Type.ToString();
            if (region.Type == RegionType.Expand) {
                regionTypeText += $" - {region.ExpandLocation.ExpandType}";
            }

            Program.GraphicalDebugger.AddLink(region.Center.ToVector3(), offsetRegionCenter, color: regionColor, withText: false);
            Program.GraphicalDebugger.AddTextGroup(
                new[]
                {
                    $"R{regionIndex} ({regionTypeText})",
                    $"Self: {GetForceLevelLabel(region, Alliance.Self)} ({GetForce(region, Alliance.Self):F2})",
                    $"Enemy: {GetForceLevelLabel(region, Alliance.Enemy)} ({GetForce(region, Alliance.Enemy):F2})",
                },
                size: 14, worldPos: offsetRegionCenter.ToPoint(), color: regionColor);

            foreach (var neighbor in region.Neighbors) {
                var neighborOffsetCenter = neighbor.Region.Center.ToVector3(zOffset: zOffset);
                var regionSizeRatio = (float)region.Cells.Count / (region.Cells.Count + neighbor.Region.Cells.Count);
                var lineEnd = Vector3.Lerp(offsetRegionCenter, neighborOffsetCenter, regionSizeRatio);
                Program.GraphicalDebugger.AddLine(offsetRegionCenter, lineEnd, color: regionColor);
            }

            regionIndex++;
        }
    }

    /// <summary>
    /// Returns a string label associated with the region's force
    /// </summary>
    /// <param name="region">The region to get the label for</param>
    /// <param name="alliance">The alliance to consider the force of</param>
    /// <returns>A string that describes the force of the region</returns>
    private static string GetForceLevelLabel(Region region, Alliance alliance) {
        var force = GetForce(region, alliance);

        return force switch
        {
            >= ForceLevel.Lethal => "Lethal",
            >= ForceLevel.Strong => "Strong",
            >= ForceLevel.Medium => "Medium",
            >= ForceLevel.Neutral => "Neutral",
            _ => "Weak"
        };
    }
}
