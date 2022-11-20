using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.MapKnowledge;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.GameSense.RegionTracking;

public class RegionTracker : INeedUpdating {
    public static RegionTracker Instance { get; private set; } = new RegionTracker();

    private bool _isInitialized = false;

    // TODO GD Make this pretty
    private IEnumerable<IRegionsEvaluator> Evaluators => _regionForceEvaluators.Values
        .Concat(_regionValueEvaluators.Values)
        .Concat(new[] { _regionDefenseEvaluator });

    private readonly Dictionary<Alliance, IRegionsEvaluator> _regionForceEvaluators = new Dictionary<Alliance, IRegionsEvaluator>
    {
        { Alliance.Self, new RegionsForceEvaluator(Alliance.Self) },
        { Alliance.Enemy, new RegionsForceEvaluator(Alliance.Enemy) },
    };

    private readonly Dictionary<Alliance, IRegionsEvaluator> _regionValueEvaluators = new Dictionary<Alliance, IRegionsEvaluator>
    {
        { Alliance.Self, new RegionsValueEvaluator(Alliance.Self) },
        { Alliance.Enemy, new RegionsValueEvaluator(Alliance.Enemy) },
    };

    private readonly IRegionsEvaluator _regionDefenseEvaluator = new RegionDefenseEvaluator();

    // TODO GD This might not live in here
    public static class Force {
        public const float None = 0f;
        public const float Unknown = 1f;
        public const float Neutral = 1f;
        public const float Medium = 2f;
        public const float Strong = 5f;
        public const float Lethal = 15f;
    }

    // TODO GD This might not live in here
    public static class Value {
        public const float None = 0f;
        public const float Unknown = 1f;
        public const float Intriguing = 1f;
        public const float Valuable = 2f;
        public const float Prized = 5f;
        public const float Jackpot = 15f;
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
    /// <param name="normalized">Whether or not to get the normalized force between 0 and 1</param>
    /// <returns>The force of the position's region</returns>
    public static float GetForce(Vector2 position, Alliance alliance, bool normalized = false) {
        return GetForce(position.GetRegion(), alliance, normalized);
    }

    /// <summary>
    /// Gets the force of a region
    /// </summary>
    /// <param name="region">The region to get the force of</param>
    /// <param name="alliance">The alliance to get the force of</param>
    /// <param name="normalized">Whether or not to get the normalized force between 0 and 1</param>
    /// <returns>The force of the region</returns>
    public static float GetForce(Region region, Alliance alliance, bool normalized = false) {
        if (!Instance._regionForceEvaluators.ContainsKey(alliance)) {
            Logger.Error("Cannot get force for alliance {0}. We don't track that", alliance);
        }

        return Instance._regionForceEvaluators[alliance].GetEvaluation(region, normalized);
    }

    /// <summary>
    /// Gets the value associated with the region of a given position
    /// </summary>
    /// <param name="position">The position to get the value of</param>
    /// <param name="alliance">The alliance to get the value of</param>
    /// <param name="normalized">Whether or not to get the normalized value between 0 and 1</param>
    /// <returns>The value of the position's region</returns>
    public static float GetValue(Vector2 position, Alliance alliance, bool normalized = false) {
        return GetValue(position.GetRegion(), alliance, normalized);
    }

    /// <summary>
    /// Gets the value of a region
    /// </summary>
    /// <param name="region">The region to get the value of</param>
    /// <param name="alliance">The alliance to get the value of</param>
    /// <param name="normalized">Whether or not to get the normalized value between 0 and 1</param>
    /// <returns>The value of the region</returns>
    public static float GetValue(Region region, Alliance alliance, bool normalized = false) {
        if (!Instance._regionValueEvaluators.ContainsKey(alliance)) {
            Logger.Error("Cannot get value for alliance {0}. We don't track that", alliance);
        }

        return Instance._regionValueEvaluators[alliance].GetEvaluation(region, normalized);
    }

    /// <summary>
    /// Gets the defense score of the region.
    /// The defense score represents valuable defending this region is
    /// </summary>
    /// <param name="region">The region to get the defense score of</param>
    /// <returns>The defense score of the given region</returns>
    public static float GetDefenseScore(Region region) {
        return Instance._regionDefenseEvaluator.GetEvaluation(region);
    }

    public void Reset() {
        Instance = new RegionTracker();
    }

    public void Update(ResponseObservation observation) {
        if (!RegionAnalyzer.IsInitialized) {
            return;
        }

        if (!_isInitialized) {
            InitEvaluators();
        }

        UpdateEvaluations();

        DrawRegionsSummary();
        DrawRegionDefenseScores();
    }

    private void InitEvaluators() {
        foreach (var evaluator in Evaluators) {
            evaluator.Init(RegionAnalyzer.Regions);
        }

        _isInitialized = true;
    }

    private void UpdateEvaluations() {
        foreach (var evaluator in Evaluators) {
            evaluator.Evaluate();
        }
    }

    /// <summary>
    /// <para>Draws a marker over each region and links with neighbors.</para>
    /// <para>The marker also indicates the force of each alliance in the region.</para>
    /// <para>Each region gets a different color using the color pool.</para>
    /// </summary>
    private static void DrawRegionsSummary() {
        if (!DebuggingFlagsTracker.ActiveDebuggingFlags.Contains(DebuggingFlags.Regions)) {
            return;
        }

        var regionIndex = 0;
        foreach (var region in RegionAnalyzer.Regions) {
            // TODO GD Set colors on each region directly, with a color different from its neighbors
            var regionColor = RegionColors[regionIndex % RegionColors.Count];

            var regionTypeText = region.Type.ToString();
            if (region.Type == RegionType.Expand) {
                regionTypeText += $" - {region.ExpandLocation.ExpandType}";
            }

            var obstructedText = region.IsObstructed ? "OBSTRUCTED" : "";

            DrawRegionMarker(region, regionColor, new[]
            {
                $"R{regionIndex} ({regionTypeText}) {obstructedText}",
                $"Self:  {GetForceLabel(region, Alliance.Self),-7} ({GetForce(region, Alliance.Self),5:F2}) | {GetValueLabel(region, Alliance.Self),-10} ({GetValue(region, Alliance.Self),5:F2})",
                $"Enemy: {GetForceLabel(region, Alliance.Enemy),-7} ({GetForce(region, Alliance.Enemy),5:F2}) | {GetValueLabel(region, Alliance.Enemy),-10} ({GetValue(region, Alliance.Enemy),5:F2})",
            }, textXOffset: -3f);

            regionIndex++;
        }
    }

    // TODO GD This might not live in here
    /// <summary>
    /// Returns a string label associated with the region's force
    /// </summary>
    /// <param name="region">The region to get the label for</param>
    /// <param name="alliance">The alliance to consider the force of</param>
    /// <returns>A string that describes the force of the region</returns>
    private static string GetForceLabel(Region region, Alliance alliance) {
        var force = GetForce(region, alliance);

        return force switch
        {
            >= Force.Lethal => "Lethal",
            >= Force.Strong => "Strong",
            >= Force.Medium => "Medium",
            >= Force.Neutral => "Neutral",
            _ => "Weak"
        };
    }

    // TODO GD This might not live in here
    /// <summary>
    /// Returns a string label associated with the region's value
    /// </summary>
    /// <param name="region">The region to get the label for</param>
    /// <param name="alliance">The alliance to consider the value of</param>
    /// <returns>A string that describes the value of the region</returns>
    private static string GetValueLabel(Region region, Alliance alliance) {
        var value = GetValue(region, alliance);

        return value switch
        {
            >= Value.Jackpot => "Jackpot",
            >= Value.Prized => "Prized",
            >= Value.Valuable => "Valuable",
            >= Value.Intriguing => "Intriguing",
            _ => "No Value"
        };
    }

    private static void DrawRegionDefenseScores() {
        if (!DebuggingFlagsTracker.ActiveDebuggingFlags.Contains(DebuggingFlags.Defense)) {
            return;
        }

        var allScores = RegionAnalyzer.Regions.Select(GetDefenseScore).ToList();
        var minScore = allScores.Min();
        var maxScore = allScores.Max();

        foreach (var region in RegionAnalyzer.Regions) {
            var defenseScore = GetDefenseScore(region);
            var normalizedDefenseScore = MathUtils.Normalize(defenseScore, minScore, maxScore);
            var color = Colors.Gradient(Colors.DarkGrey, Colors.BrightGreen, normalizedDefenseScore);

            if (region.Type == RegionType.Ramp) {
                foreach (var cell in region.Cells) {
                    // Spheres are more visible then squares for ramps
                    Program.GraphicalDebugger.AddGridSphere(cell.ToVector3(), color);
                }
            }
            else {
                foreach (var cell in region.Cells) {
                    Program.GraphicalDebugger.AddGridSquare(cell.ToVector3(), color);
                }
            }

            DrawRegionMarker(region, Colors.Green, new []{ $"Defense score: {defenseScore,4:F2} ({normalizedDefenseScore,5:P2})" }, textXOffset: -2f);
        }
    }

    private static void DrawRegionMarker(Region region, Color regionColor, string[] texts, float textXOffset = 0f) {
        const int zOffset = 5;
        var offsetRegionCenter = region.Center.ToVector3(zOffset: zOffset);

        // Draw the marker
        Program.GraphicalDebugger.AddLink(region.Center.ToVector3(), offsetRegionCenter, color: regionColor);
        Program.GraphicalDebugger.AddTextGroup(texts, size: 14, worldPos: offsetRegionCenter.ToPoint(xOffset: textXOffset), color: regionColor);

        // Draw lines to neighbors
        foreach (var neighbor in region.Neighbors) {
            var neighborOffsetCenter = neighbor.Region.Center.ToVector3(zOffset: zOffset);
            var regionSizeRatio = (float)region.Cells.Count / (region.Cells.Count + neighbor.Region.Cells.Count);
            var lineEnd = Vector3.Lerp(offsetRegionCenter, neighborOffsetCenter, regionSizeRatio);
            Program.GraphicalDebugger.AddLine(offsetRegionCenter, lineEnd, color: regionColor);
        }
    }
}
