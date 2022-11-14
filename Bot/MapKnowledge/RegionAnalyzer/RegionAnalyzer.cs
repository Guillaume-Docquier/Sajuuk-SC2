using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using SC2APIProtocol;

namespace Bot.MapKnowledge;

public class RegionAnalyzer: INeedUpdating {
    public static readonly RegionAnalyzer Instance = new RegionAnalyzer();

    public static bool IsInitialized { get; private set; } = false;
    private static Dictionary<Vector2, Region> _regionsMap;
    private static RegionData _regionData;

    public static List<Region> Regions => _regionData.Regions;

    private const int MinRampSize = 6;
    private const int RegionMinPoints = 6;
    private const float RegionZMultiplier = 8;
    private static readonly float DiagonalDistance = (float)Math.Sqrt(2);

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

    private RegionAnalyzer() {}

    public void Reset() {
        IsInitialized = false;
        _regionsMap = null;
        _regionData = null;
    }

    /// <summary>
    /// <para>Analyzes the map to find ramps and do region decomposition</para>
    /// <para>There should be at least 1 region per expand location and regions are always separated by ramps or choke points.</para>
    /// </summary>
    public void Update(ResponseObservation observation) {
        if (!ExpandAnalyzer.IsInitialized) {
            return;
        }

        if (IsInitialized) {
            Debug();

            return;
        }

        var regionsData = RegionDataStore.Load(Controller.GameInfo.MapName);
        if (regionsData != null) {
            Logger.Info("Initializing RegionAnalyzer from precomputed data for {0}", Controller.GameInfo.MapName);

            _regionData = regionsData;

            _regionsMap = BuildRegionsMap(_regionData.Regions);
            _regionData.Regions.ForEach(region => region.Init(computeObstruction: false));

            Logger.Metric("{0} regions ({1} obstructed), {2} ramps, {3} unclassified cells and {4} choke points", _regionData.Regions.Count, _regionData.Regions.Count(region => region.IsObstructed), _regionData.Ramps.Count, _regionData.Noise.Count, _regionData.ChokePoints.Count);
            Logger.Success("Regions loaded from file");
            IsInitialized = true;

            return;
        }

        var map = GenerateMap();
        Logger.Info("Starting region analysis on {0} cells ({1}x{2})", map.Count, MapAnalyzer.MaxX, MapAnalyzer.MaxY);

        var (potentialRegions, regionsNoise) = ComputePotentialRegions(map);
        var (ramps, rampsNoise) = ComputeRamps(regionsNoise);
        var noise = rampsNoise.Select(mapCell => mapCell.Position.ToVector2()).ToList(); // TODO GD Noise should be added to any region that it touches
        var chokePoints = ComputePotentialChokePoints();

        var regions = BuildRegions(potentialRegions, ramps, chokePoints);
        _regionData = new RegionData(regions, ramps, noise, chokePoints);

        _regionsMap = BuildRegionsMap(regions);
        regions.ForEach(region => region.Init(computeObstruction: true));

        RegionDataStore.Save(Controller.GameInfo.MapName, _regionData);

        Logger.Metric("{0} regions ({1} obstructed), {2} ramps, {3} unclassified cells and {4} choke points", _regionData.Regions.Count, _regionData.Regions.Count(region => region.IsObstructed), _regionData.Ramps.Count, _regionData.Noise.Count, _regionData.ChokePoints.Count);
        Logger.Success("Region analysis done and saved");
        IsInitialized = true;
    }

    /// <summary>
    /// Get the region outside of the natural for yourself or the enemy.
    /// </summary>
    /// <param name="alliance">Yourself or the enemy</param>
    /// <returns>The region outside of the natural</returns>
    public static Region GetNaturalExitRegion(Alliance alliance) {
        var natural = ExpandAnalyzer.GetExpand(alliance, ExpandType.Natural);

        return Regions
            .Where(region => region.Type == RegionType.OpenArea)
            .MinBy(region => region.Center.DistanceTo(natural.Position))!;
    }

    /// <summary>
    /// Gets the Region of a given position
    /// </summary>
    /// <param name="position">The position to get the Region of</param>
    /// <returns>The Region of the given position</returns>
    public static Region GetRegion(Vector3 position) {
        return GetRegion(position.ToVector2());
    }

    /// <summary>
    /// Gets the Region of a given position
    /// </summary>
    /// <param name="position">The position to get the Region of</param>
    /// <returns>The Region of the given position</returns>
    public static Region GetRegion(Vector2 position) {
        if (_regionsMap.TryGetValue(position.AsWorldGridCenter(), out var region)) {
            return region;
        }

        if (MapAnalyzer.IsWalkable(position) && !_regionData.Noise.Contains(position)) {
            Logger.Warning("Region not found for walkable position {0}", position);
        }

        return null;
    }

    /// <summary>
    /// Enables graphical debugging of the RegionAnalyzer's data based on debug flags
    /// </summary>
    private static void Debug() {
        if (DebuggingFlagsTracker.ActiveDebuggingFlags.Contains(DebuggingFlags.RegionCells)) {
            DrawRegions();
            DrawNoise();
        }

        if (DebuggingFlagsTracker.ActiveDebuggingFlags.Contains(DebuggingFlags.ChokePoints)) {
            DrawChokePoints();
        }
    }

    /// <summary>
    /// <para>Draws a square on each region's cells.</para>
    /// <para>Each region gets a different color using the color pool.</para>
    /// <para>Each cell also gets a text 'EX', where E stands for 'Expand' and X is the region index.</para>
    /// </summary>
    private static void DrawRegions() {
        var regionIndex = 0;
        foreach (var region in _regionData.Regions) {
            var regionColor = RegionColors[regionIndex % RegionColors.Count];
            var frontier = region.Neighbors.SelectMany(neighboringRegion => neighboringRegion.Frontier).ToList();

            foreach (var position in region.Cells.Except(frontier)) {
                Program.GraphicalDebugger.AddText($"{regionIndex}", size: 12, worldPos: position.ToVector3().ToPoint(), color: regionColor);
                Program.GraphicalDebugger.AddGridSquare(position.ToVector3(), regionColor);
            }

            foreach (var position in frontier) {
                Program.GraphicalDebugger.AddText($"F{regionIndex}", size: 12, worldPos: position.ToVector3().ToPoint(), color: regionColor);
                Program.GraphicalDebugger.AddGridSphere(position.ToVector3(), regionColor);
            }

            regionIndex++;
        }
    }

    /// <summary>
    /// <para>Draws a red square on each noise cell.</para>
    /// <para>A noise cell is a cell that isn't part of a region or ramp.</para>
    /// <para>Each cell also gets a text '?'.</para>
    /// </summary>
    private static void DrawNoise() {
        foreach (var position in _regionData.Noise) {
            Program.GraphicalDebugger.AddText("?", size: 12, worldPos: position.ToVector3().ToPoint(), color: Colors.Red);
            Program.GraphicalDebugger.AddGridSphere(position.ToVector3(), Colors.Red);
        }
    }

    /// <summary>
    /// Draws all the choke points
    /// </summary>
    private static void DrawChokePoints() {
        foreach (var chokePoint in _regionData.ChokePoints) {
            Program.GraphicalDebugger.AddPath(chokePoint.Edge.Select(edge => edge.ToVector3()).ToList(), Colors.LightRed, Colors.LightRed);
        }
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
    /// <para>Computes the potential regions by using clustering and highly penalizing height differences.</para>
    /// <para>This penalty will allow us to isolate ramps by turning them into noise, which we can cluster properly afterwards.</para>
    /// <para>We will later on try to break down each region into sub regions, because not all regions are separated by ramps.</para>
    /// </summary>
    /// <returns>
    /// The potential regions and the cells that are not part of any region.
    /// </returns>
    private static (List<HashSet<Vector2>> potentialRegions, List<MapCell> regionsNoise) ComputePotentialRegions(List<MapCell> cells) {
        cells.ForEach(mapCell => {
            // Highly penalize height differences
            var trickPosition = mapCell.Position;
            trickPosition.Z *= RegionZMultiplier;
            mapCell.Position = trickPosition;
        });

        var (clusters, noise) = Clustering.DBSCAN(cells, epsilon: DiagonalDistance + 0.04f, minPoints: RegionMinPoints);
        var potentialRegions = clusters.Select(cluster => cluster.Select(mapCell => mapCell.Position.ToVector2()).ToHashSet()).ToList();

        return (potentialRegions, noise);
    }

    /// <summary>
    /// Identify ramps given cells that are not part of any regions using clustering.
    /// </summary>
    /// <returns>
    /// The ramps and the cells that are not part of any ramp.
    /// </returns>
    private static (List<HashSet<Vector2>> ramps, IEnumerable<MapCell> rampsNoise) ComputeRamps(List<MapCell> cells) {
        cells.ForEach(mapCell => mapCell.Position = mapCell.Position with { Z = 0 }); // Ignore Z

        var (clusters, noise) = Clustering.DBSCAN(cells, epsilon: DiagonalDistance, minPoints: MinRampSize);
        var ramps = clusters.Select(cluster => cluster.Select(mapCell => mapCell.Position.ToVector2()).ToHashSet()).ToList();

        return (ramps, noise);
    }

    private static List<ChokePoint> ComputePotentialChokePoints() {
        return RayCastingChokeFinder.FindChokePoints();
    }

    private static List<Region> BuildRegions(List<HashSet<Vector2>> potentialRegions, List<HashSet<Vector2>> ramps, List<ChokePoint> potentialChokePoints) {
        var regions = new List<Region>();
        foreach (var region in potentialRegions) {
            var subregions = BreakDownIntoSubregions(region.ToHashSet(), potentialChokePoints);
            regions.AddRange(subregions.Select(subregion => new Region(subregion, RegionType.Unknown)));
        }

        regions.AddRange(ramps.Select(ramp => new Region(ramp, RegionType.Ramp)));

        return regions;
    }

    /// <summary>
    /// <para>Break down a given region into subregions based on choke points</para>
    /// <para>We might use more than one choke point to break down a single region</para>
    /// <para>Regions that are broken down must be big enough to be considered a valid split</para>
    /// </summary>
    /// <returns>
    /// A list of subregions.
    /// </returns>
    private static List<List<Vector2>> BreakDownIntoSubregions(IReadOnlySet<Vector2> region, List<ChokePoint> potentialChokePoints) {
        // Get chokes in region
        // Consider shortest chokes first
        var chokesInRegion = potentialChokePoints
            .Where(chokePoint => chokePoint.Edge.Any(region.Contains))
            .OrderBy(chokePoint => chokePoint.Length)
            .ToList();

        // Try to split the region into two reasonably sized subregions using choke points
        // Sometimes we will need more than 1 choke to split a region into two
        var nbChokesToConsider = 1;
        while (nbChokesToConsider <= chokesInRegion.Count) {
            var chokePointCombinations = Combinations(chokesInRegion, nbChokesToConsider).Select(setOfChokes => setOfChokes.ToList());
            foreach (var chokePointCombination in chokePointCombinations) {
                var (subregion1, subregion2) = SplitRegion(region, chokePointCombination.SelectMany(choke => choke.Edge).ToList());

                var maxChokeLength = chokePointCombination.Max(choke => choke.Edge.Count);
                if (IsValidSplit(subregion1, maxChokeLength) && IsValidSplit(subregion2, maxChokeLength)) {
                    return BreakDownIntoSubregions(subregion1, potentialChokePoints).Concat(BreakDownIntoSubregions(subregion2, potentialChokePoints)).ToList();
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

    /// <summary>
    /// Determines if a subregion is the result of a valid split given the cut length
    /// A subregion should be a single cluster of cells that's big enough compared to the cut
    /// </summary>
    /// <param name="subregion"></param>
    /// <param name="cutLength"></param>
    /// <returns>True if the split is valid, false otherwise</returns>
    private static bool IsValidSplit(IReadOnlyCollection<Vector2> subregion, float cutLength) {
        // If the split region is too small compared to the cut, it might not be worth a cut
        if (subregion.Count <= Math.Max(10, cutLength * cutLength / 2)) {
            return false;
        }

        // A region should form a single cluster of cells
        var clusteringResult = Clustering.DBSCAN(subregion.Select(cell => new MapCell(cell)).ToList(), 1, 2);
        if (clusteringResult.clusters.Count > 1 || clusteringResult.noise.Count > 0) {
            return false;
        }

        return true;
    }

    // TODO GD Code this yourself :rofl: this looks... questionable
    private static IEnumerable<IEnumerable<T>> Combinations<T>(IEnumerable<T> elements, int k)
    {
        if (k == 0) {
            return new[] { Array.Empty<T>() };
        }

        return elements.SelectMany((e, i) => Combinations(elements.Skip(i + 1), k - 1).Select(c => new[] { e }.Concat(c)));
    }

    private static Dictionary<Vector2, Region> BuildRegionsMap(List<Region> regions) {
        var regionsMap = new Dictionary<Vector2, Region>();
        foreach (var region in regions) {
            foreach (var cell in region.Cells) {
                regionsMap[cell] = region;
            }
        }

        return regionsMap;
    }
}
