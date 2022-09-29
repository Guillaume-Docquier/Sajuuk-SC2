using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.MapKnowledge;

public class RegionAnalyzer: INeedUpdating {
    private const bool DrawEnabled = true;

    private const int MinRampSize = 5;
    private const int RegionMinPoints = 6;
    private const float RegionZMultiplier = 8;
    private static readonly float DiagonalDistance = (float)Math.Sqrt(2);

    public static readonly RegionAnalyzer Instance = new RegionAnalyzer();

    public static List<HashSet<Vector2>> Regions = new List<HashSet<Vector2>>();
    public static List<HashSet<Vector2>> Ramps = new List<HashSet<Vector2>>();
    public static List<Vector2> Noise = new List<Vector2>();
    public static List<ChokePoint> ChokePoints = new List<ChokePoint>();

    private static bool _isInitialized = false;

    private readonly List<Color> _regionColors = new List<Color>
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

    /// <summary>
    /// <para>Analyzes the map to find ramps and do region decomposition using clustering techniques.</para>
    /// <para>There should be at least 1 region per expand location and ramps always separate two regions.</para>
    /// </summary>
    public void Update(ResponseObservation observation) {
        if (!ExpandAnalyzer.IsInitialized) {
            return;
        }

        if (_isInitialized) {
            if (Program.DebugEnabled && DrawEnabled) {
                DrawRegions();
                DrawRamps();
                DrawNoise();
                DrawChokePoints();
            }

            return;
        }

        var regionsData = RegionDataStore.Load(Controller.GameInfo.MapName);
        if (regionsData != null) {
            Logger.Info("Initializing RegionAnalyzer from precomputed data for {0}", Controller.GameInfo.MapName);

            Regions = regionsData.Regions;
            Ramps = regionsData.Ramps;
            Noise = regionsData.Noise;
            ChokePoints = regionsData.ChokePoints;

            Logger.Info("{0} regions, {1} ramps, {2} unclassified cells and {3} choke points", Regions.Count, Ramps.Count, Noise.Count, ChokePoints.Count);
            _isInitialized = true;

            return;
        }

        var map = GenerateMap();

        Logger.Info("Starting region analysis on {0} cells ({1}x{2})", map.Count, MapAnalyzer.MaxX, MapAnalyzer.MaxY);

        var regionsNoise = InitRegions(map);
        var rampsNoise = InitRamps(regionsNoise);
        Noise.AddRange(rampsNoise.Select(mapCell => mapCell.Position.ToVector2()));
        InitChokePoints();
        InitSubregions();

        RegionDataStore.Save(Controller.GameInfo.MapName, new RegionData(Regions, Ramps, Noise, ChokePoints));

        Logger.Info("Region analysis done and saved");

        Logger.Info("{0} regions, {1} ramps, {2} unclassified cells and {3} choke points", Regions.Count, Ramps.Count, Noise.Count, ChokePoints.Count);
        _isInitialized = true;
    }

    /// <summary>
    /// <para>Draws a square on each region's cells.</para>
    /// <para>Each region gets a different color using the color pool.</para>
    /// <para>Each cell also gets a text 'EX', where E stands for 'Expand' and X is the region index.</para>
    /// </summary>
    private void DrawRegions() {
        var regionIndex = 0;
        foreach (var region in Regions) {
            foreach (var position in region) {
                Program.GraphicalDebugger.AddText($"E{regionIndex}", size: 12, worldPos: position.ToVector3().WithWorldHeight().ToPoint(), color: _regionColors[regionIndex % _regionColors.Count]);
                Program.GraphicalDebugger.AddGridSquare(position.ToVector3().WithWorldHeight(), _regionColors[regionIndex % _regionColors.Count]);
            }

            regionIndex++;
        }
    }

    /// <summary>
    /// <para>Draws a sphere on each ramp's cells.</para>
    /// <para>Each ramp gets a different color using the color pool.</para>
    /// <para>Each cell also gets a text 'RX', where E stands for 'Ramp' and X is the ramp index.</para>
    /// </summary>
    private void DrawRamps() {
        var rampIndex = 0;
        foreach (var ramp in Ramps) {
            foreach (var position in ramp) {
                Program.GraphicalDebugger.AddText($"R{rampIndex}", size: 12, worldPos: position.ToVector3().WithWorldHeight().ToPoint(), color: _regionColors[rampIndex % _regionColors.Count]);
                Program.GraphicalDebugger.AddGridSphere(position.ToVector3().WithWorldHeight(), _regionColors[rampIndex % _regionColors.Count]);
            }

            rampIndex++;
        }
    }

    /// <summary>
    /// <para>Draws a red square on each noise cell.</para>
    /// <para>A noise cell is a cell that isn't part of a region or ramp.</para>
    /// <para>Each cell also gets a text '?'.</para>
    /// </summary>
    private static void DrawNoise() {
        foreach (var position in Noise) {
            Program.GraphicalDebugger.AddText("?", size: 12, worldPos: position.ToVector3().WithWorldHeight().ToPoint(), color: Colors.Red);
            Program.GraphicalDebugger.AddGridSphere(position.ToVector3().WithWorldHeight(), Colors.Red);
        }
    }

    private static void DrawChokePoints() {
        foreach (var chokePoint in ChokePoints) {
            DrawChokePoint(chokePoint);
        }
    }

    private static void DrawChokePoint(ChokePoint chokePoint, Color color = null) {
        Program.GraphicalDebugger.AddPath(chokePoint.Edge.Select(edge => edge.ToVector3().WithWorldHeight()).ToList(), color ?? Colors.LightRed, color ?? Colors.LightRed);
    }

    /// <summary>
    /// Generates a list of MapCell representing each playable tile in the map.
    /// </summary>
    private static List<MapCell> GenerateMap() {
        var map = new List<MapCell>();
        for (var x = 0; x < MapAnalyzer.MaxX; x++) {
            for (var y = 0; y < MapAnalyzer.MaxY; y++) {
                var mapCell = new MapCell(x, y);
                if (MapAnalyzer.IsWalkable(mapCell.Position, includeObstacles: false)) {
                    map.Add(mapCell);
                }
            }
        }

        return map;
    }

    /// <summary>
    /// <para>Computes the regions by using clustering and highly penalizing height differences.</para>
    /// <para>This penalty will allow us to isolate ramps by turning them into noise, which we can cluster properly afterwards.</para>
    /// <para>We will then try to break down each region into sub regions, because not all regions are separated by ramps.</para>
    /// </summary>
    /// <returns>
    /// Cells that are not part of any region.
    /// </returns>
    private static List<MapCell> InitRegions(List<MapCell> cells) {
        cells.ForEach(mapCell => {
            // Highly penalize height differences
            var trickPosition = mapCell.Position;
            trickPosition.Z *= RegionZMultiplier;
            mapCell.Position = trickPosition;
        });

        var regionClusteringResult = Clustering.DBSCAN(cells, epsilon: DiagonalDistance + 0.04f, minPoints: RegionMinPoints);
        Regions.AddRange(regionClusteringResult.clusters.Select(cluster => cluster.Select(mapCell => mapCell.Position.ToVector2()).ToHashSet()).ToList());

        return regionClusteringResult.noise;
    }

    /// <summary>
    /// Identify ramps given cells that are not part of any regions using clustering.
    /// </summary>
    /// <returns>
    /// Cells that are not part of any ramp.
    /// </returns>
    private static IEnumerable<MapCell> InitRamps(List<MapCell> cells) {
        cells.ForEach(mapCell => mapCell.Position = mapCell.Position.WithWorldHeight()); // Reset the Z

        var rampClusteringResult = Clustering.DBSCAN(cells, epsilon: DiagonalDistance * 2, minPoints: MinRampSize);
        foreach (var ramp in rampClusteringResult.clusters) {
            Ramps.Add(ramp.Select(cell => cell.Position.ToVector2()).ToHashSet());
        }

        return rampClusteringResult.noise;
    }

    private static void InitChokePoints() {
        ChokePoints.AddRange(GridScanChokeFinder.FindChokePoints());
    }

    private static void InitSubregions() {
        var regions = Regions.ToList();
        Regions.Clear();
        foreach (var region in regions) {
            var subregions = BreakDownIntoSubregions(region.Select(cell => cell).ToHashSet());
            foreach (var subregion in subregions) {
                Regions.Add(subregion.ToHashSet());
            }
        }
    }

    /// <summary>
    /// <para>Break down a given region into subregions based on choke points</para>
    /// <para>We might use more than one choke point to break down a single region</para>
    /// <para>Regions that are broken down must be big enough to be considered a valid split</para>
    /// </summary>
    /// <returns>
    /// A list of subregions.
    /// </returns>
    private static List<List<Vector2>> BreakDownIntoSubregions(HashSet<Vector2> region) {
        // Get chokes in region
        // Order by length ascending
        var chokesInRegion = ChokePoints
            .Where(chokePoint => chokePoint.Edge.Any(region.Contains))
            .OrderBy(chokePoint => chokePoint.Length)
            .ToList();

        // Keep and split again if split separates into chunks bigger than choke width squared
        var nbChokesToConsider = 1;
        while (nbChokesToConsider <= chokesInRegion.Count) {
            // Sometimes we will need more than 1 choke to split a region
            var chokePointCombinations = Combinations(chokesInRegion, nbChokesToConsider).Select(setOfChokes => setOfChokes.ToList());

            foreach (var chokePoints in chokePointCombinations) {
                var (subregion1, subregion2) = SplitRegion(region, chokePoints.SelectMany(choke => choke.Edge).ToList());

                // TODO GD Only keep chokes that produce valid splits
                var maxChokeLength = chokePoints.Max(choke => choke.Edge.Count);
                if (IsValidSplit(subregion1, maxChokeLength) && IsValidSplit(subregion2, maxChokeLength)) {
                    return BreakDownIntoSubregions(subregion1).Concat(BreakDownIntoSubregions(subregion2)).ToList();
                }
            }

            nbChokesToConsider++;
        }

        return new List<List<Vector2>> { region.ToList() };
    }

    private static (HashSet<Vector2> subregion1, HashSet<Vector2> subregion2) SplitRegion(IReadOnlySet<Vector2> region, IReadOnlyCollection<Vector2> separations) {
        var startingPoint = region.First(point => !separations.Contains(point));

        var subregion1 = new HashSet<Vector2>();

        var pointsToExplore = new Queue<Vector2>();
        pointsToExplore.Enqueue(startingPoint);

        while (pointsToExplore.Any()) {
            var point = pointsToExplore.Dequeue();
            if (subregion1.Add(point)) {
                var nextNeighbors = point.GetNeighbors()
                    .Where(neighbor => neighbor.DistanceTo(point) <= 1) // Disallow diagonals
                    .Where(region.Contains)
                    .Where(neighbor => !separations.Contains(neighbor))
                    .Where(neighbor => !subregion1.Contains(neighbor));

                foreach (var neighbor in nextNeighbors) {
                    pointsToExplore.Enqueue(neighbor);
                }
            }
        }

        var subregion2 = region.Except(subregion1).ToHashSet();

        return (subregion1, subregion2);
    }

    private static bool IsValidSplit(IReadOnlyCollection<Vector2> subregion, float minDiameter) {
        // If the split region is smaller than the choke width^2, then the choke might not be one for real
        return subregion.Count > minDiameter * minDiameter;
    }

    // TODO GD Code this yourself :rofl: this looks... questionable
    public static IEnumerable<IEnumerable<T>> Combinations<T>(IEnumerable<T> elements, int k)
    {
        if (k == 0) {
            return new[] { Array.Empty<T>() };
        }

        return elements.SelectMany((e, i) => Combinations(elements.Skip(i + 1), k - 1).Select(c => new[] { e }.Concat(c)));
    }
}
