using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Algorithms;
using Bot.Debugging;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.MapKnowledge;

// TODO GD Make two classes: The analyzer and the tracker
// Analysis should be run manually, and the tracker should be able to load the persisted data before entering the game
public class RegionAnalyzer: INeedUpdating {
    public static readonly RegionAnalyzer Instance = new RegionAnalyzer(DebuggingFlagsTracker.Instance, MapAnalyzer.Instance);

    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;
    private readonly IMapAnalyzer _mapAnalyzer;

    private readonly RegionDataStore _regionDataStore;
    private readonly RayCastingChokeFinder _rayCastingChokeFinder;

    public static bool IsInitialized { get; private set; } = false;
    private Dictionary<Vector2, Region> _regionsLookupMap;
    private RegionData _regionData;

    public List<Region> Regions => _regionData.Regions;

    private const int RegionMinPoints = 6;
    private const float RegionZMultiplier = 8;
    private readonly float _diagonalDistance = (float)Math.Sqrt(2);

    private RegionAnalyzer(IDebuggingFlagsTracker debuggingFlagsTracker, IMapAnalyzer mapAnalyzer) {
        _debuggingFlagsTracker = debuggingFlagsTracker;
        _mapAnalyzer = mapAnalyzer;

        // TODO GD Inject this as well, probably
        _regionDataStore = new RegionDataStore(_mapAnalyzer);
        _rayCastingChokeFinder = new RayCastingChokeFinder(_mapAnalyzer);
    }

    public void Reset() {
        IsInitialized = false;
        _regionsLookupMap = null;
        _regionData = null;
    }

    /// <summary>
    /// <para>Analyzes the map to find ramps and do region decomposition</para>
    /// <para>There should be at least 1 region per expand location and regions are always separated by ramps or choke points.</para>
    /// </summary>
    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        if (!ExpandAnalyzer.IsInitialized) {
            return;
        }

        if (IsInitialized) {
            Debug();

            return;
        }

        var regionsData = _regionDataStore.Load(Controller.GameInfo.MapName);
        if (regionsData != null) {
            Logger.Info("Initializing RegionAnalyzer from precomputed data for {0}", Controller.GameInfo.MapName);

            _regionData = regionsData;

            _regionsLookupMap = BuildRegionsLookupMap(_regionData.Regions);

            for (var regionIndex = 0; regionIndex < _regionData.Regions.Count; regionIndex++) {
                _regionData.Regions[regionIndex].Init(regionIndex, computeObstruction: false);
            }

            Logger.Metric("{0} regions ({1} obstructed), {2} ramps, {3} unclassified cells and {4} choke points", _regionData.Regions.Count, _regionData.Regions.Count(region => region.IsObstructed), _regionData.Ramps.Count, _regionData.Noise.Count, _regionData.ChokePoints.Count);
            Logger.Success("Regions loaded from file");
            IsInitialized = true;

            return;
        }

        var walkableMap = GenerateWalkableMap();
        Logger.Info("Starting region analysis on {0} cells ({1}x{2})", walkableMap.Count, _mapAnalyzer.MaxX, _mapAnalyzer.MaxY);

        var rampsPotentialCells = walkableMap.Where(cell => !_mapAnalyzer.IsBuildable(cell.Position, includeObstacles: false)).ToList();
        var (ramps, rampsNoise) = ComputeRamps(rampsPotentialCells);

        var regionsPotentialCells = walkableMap
            .Where(cell => _mapAnalyzer.IsBuildable(cell.Position, includeObstacles: false))
            .Concat(rampsNoise)
            .ToList();
        var (potentialRegions, regionNoise) = ComputePotentialRegions(regionsPotentialCells);

        var noise = regionNoise.Select(mapCell => mapCell.Position.ToVector2()).ToList();

        var chokePoints = ComputePotentialChokePoints();

        var regions = BuildRegions(potentialRegions, ramps, chokePoints);
        _regionData = new RegionData(regions, ramps, noise, chokePoints);

        _regionsLookupMap = BuildRegionsLookupMap(regions);

        for (var regionIndex = 0; regionIndex < regions.Count; regionIndex++) {
            regions[regionIndex].Init(regionIndex, computeObstruction: false);
        }

        _regionDataStore.Save(Controller.GameInfo.MapName, _regionData);

        Logger.Metric("{0} regions ({1} obstructed), {2} ramps, {3} unclassified cells and {4} choke points", _regionData.Regions.Count, _regionData.Regions.Count(region => region.IsObstructed), _regionData.Ramps.Count, _regionData.Noise.Count, _regionData.ChokePoints.Count);
        Logger.Success("Region analysis done and saved");
        IsInitialized = true;
    }

    /// <summary>
    /// Get the region outside of the natural for yourself or the enemy.
    /// </summary>
    /// <param name="alliance">Yourself or the enemy</param>
    /// <returns>The region outside of the natural</returns>
    public Region GetNaturalExitRegion(Alliance alliance) {
        var natural = ExpandAnalyzer.Instance.GetExpand(alliance, ExpandType.Natural);

        return Regions
            .Where(region => region.Type == RegionType.OpenArea)
            .MinBy(region => region.Center.DistanceTo(natural.Position))!;
    }

    /// <summary>
    /// Gets the Region of a given position
    /// </summary>
    /// <param name="position">The position to get the Region of</param>
    /// <returns>The Region of the given position</returns>
    public Region GetRegion(Vector3 position) {
        return GetRegion(position.ToVector2());
    }

    /// <summary>
    /// Gets the Region of a given position
    /// </summary>
    /// <param name="position">The position to get the Region of</param>
    /// <returns>The Region of the given position</returns>
    public Region GetRegion(Vector2 position) {
        if (_regionsLookupMap.TryGetValue(position.AsWorldGridCenter(), out var region)) {
            return region;
        }

        if (_mapAnalyzer.IsWalkable(position) && !_regionData.Noise.Contains(position)) {
            Logger.Warning("Region not found for walkable position {0}", position);
        }

        return null;
    }

    /// <summary>
    /// Enables graphical debugging of the RegionAnalyzer's data based on debug flags
    /// </summary>
    private void Debug() {
        if (_debuggingFlagsTracker.IsActive(DebuggingFlags.RegionCells)) {
            DrawRegions();
            DrawNoise();
        }

        if (_debuggingFlagsTracker.IsActive(DebuggingFlags.ChokePoints)) {
            DrawChokePoints();
        }
    }

    /// <summary>
    /// <para>Draws a square on each region's cells.</para>
    /// <para>Each region gets a different color using the color pool.</para>
    /// <para>Each cell also gets a text 'EX', where E stands for 'Expand' and X is the region index.</para>
    /// </summary>
    private void DrawRegions() {
        foreach (var region in _regionData.Regions) {
            var frontier = region.Neighbors.SelectMany(neighboringRegion => neighboringRegion.Frontier).ToList();

            foreach (var position in region.Cells.Except(frontier)) {
                Program.GraphicalDebugger.AddText($"{region.Id}", size: 12, worldPos: _mapAnalyzer.WithWorldHeight(position).ToPoint(), color: region.Color);
                Program.GraphicalDebugger.AddGridSquare(_mapAnalyzer.WithWorldHeight(position), region.Color);
            }

            foreach (var position in frontier) {
                Program.GraphicalDebugger.AddText($"F{region.Id}", size: 12, worldPos: _mapAnalyzer.WithWorldHeight(position).ToPoint(), color: region.Color);
                Program.GraphicalDebugger.AddGridSphere(_mapAnalyzer.WithWorldHeight(position), region.Color);
            }
        }
    }

    /// <summary>
    /// <para>Draws a red square on each noise cell.</para>
    /// <para>A noise cell is a cell that isn't part of a region or ramp.</para>
    /// <para>Each cell also gets a text '?'.</para>
    /// </summary>
    private void DrawNoise() {
        foreach (var position in _regionData.Noise) {
            Program.GraphicalDebugger.AddText("?", size: 12, worldPos: _mapAnalyzer.WithWorldHeight(position).ToPoint(), color: Colors.Red);
            Program.GraphicalDebugger.AddGridSphere(_mapAnalyzer.WithWorldHeight(position), Colors.Red);
        }
    }

    /// <summary>
    /// Draws all the choke points
    /// </summary>
    private void DrawChokePoints() {
        foreach (var chokePoint in _regionData.ChokePoints) {
            Program.GraphicalDebugger.AddPath(chokePoint.Edge.Select(edge => _mapAnalyzer.WithWorldHeight(edge)).ToList(), Colors.LightRed, Colors.LightRed);
        }
    }

    /// <summary>
    /// Generates a list of MapCell representing each playable tile in the map.
    /// </summary>
    private List<MapCell> GenerateWalkableMap() {
        var map = new List<MapCell>();
        for (var x = 0; x < _mapAnalyzer.MaxX; x++) {
            for (var y = 0; y < _mapAnalyzer.MaxY; y++) {
                var mapCell = new MapCell(_mapAnalyzer.WithWorldHeight(new Vector2(x, y)));
                if (_mapAnalyzer.IsWalkable(mapCell.Position, includeObstacles: false)) {
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
    private (List<HashSet<Vector2>> potentialRegions, IEnumerable<MapCell> regionsNoise) ComputePotentialRegions(List<MapCell> cells) {
        cells.ForEach(mapCell => {
            // Highly penalize height differences
            var trickPosition = _mapAnalyzer.WithWorldHeight(mapCell.Position);
            trickPosition.Z *= RegionZMultiplier;

            mapCell.Position = trickPosition;
        });

        var noise = new HashSet<MapCell>();
        var clusteringResult = Clustering.Instance.DBSCAN(cells, epsilon: _diagonalDistance + 0.04f, minPoints: RegionMinPoints);
        foreach (var mapCell in clusteringResult.noise) {
            noise.Add(mapCell);
        }

        var potentialRegions = clusteringResult.clusters.Select(cluster => cluster.Select(mapCell => mapCell.Position.ToVector2()).ToHashSet()).ToList();

        // Add noise to any neighboring region
        foreach (var mapCell in noise.ToList()) {
            var mapCellNeighbors = mapCell.Position.ToVector2().GetNeighbors();
            var regionToAddTo = potentialRegions.FirstOrDefault(potentialRegion => mapCellNeighbors.Any(potentialRegion.Contains));
            if (regionToAddTo != default) {
                regionToAddTo.Add(mapCell.Position.ToVector2());
                noise.Remove(mapCell);
            }
        }

        return (potentialRegions, noise);
    }

    /// <summary>
    /// Identify ramps given cells that should be walkable but not buildable.
    /// Some noise will be produced because some unbuildable cells are vision blockers and they should be used to find regions.
    /// </summary>
    /// <returns>
    /// The ramps and the cells that are not part of any ramp.
    /// </returns>
    private (List<HashSet<Vector2>> ramps, IEnumerable<MapCell> rampsNoise) ComputeRamps(List<MapCell> cells) {
        cells.ForEach(mapCell => mapCell.Position = mapCell.Position with { Z = 0 }); // Ignore Z

        var ramps = new List<HashSet<Vector2>>();
        var noise = new HashSet<MapCell>();

        // We cluster once for an initial split
        var weakClusteringResult = Clustering.Instance.DBSCAN(cells, epsilon: 1, minPoints: 1);
        foreach (var mapCell in weakClusteringResult.noise) {
            noise.Add(mapCell);
        }

        foreach (var weakCluster in weakClusteringResult.clusters) {
            var clusterSet = weakCluster.Select(cell => cell.Position).ToHashSet();
            var maxConnections = weakCluster.Max(cell => cell.Position.GetNeighbors().Count(neighbor => clusterSet.Contains(neighbor)));
            if (maxConnections < 8) {
                // This is to make ramps work
                maxConnections = (int)Math.Floor(0.875f * maxConnections);
            }

            // Some ramps touch each other (berlingrad)
            // We do a 2nd round of clustering based on the connectivity of the cluster
            // This is because ramps have low connectivity, so we need it to be variable
            var rampClusterResult = Clustering.Instance.DBSCAN(weakCluster, epsilon: _diagonalDistance, minPoints: maxConnections);

            foreach (var mapCell in rampClusterResult.noise) {
                noise.Add(mapCell);
            }

            foreach (var rampCluster in rampClusterResult.clusters) {
                if (IsReallyARamp(rampCluster)) {
                    ramps.Add(rampCluster.Select(mapCell => mapCell.Position.ToVector2()).ToHashSet());
                }
                else {
                    foreach (var mapCell in rampCluster) {
                        noise.Add(mapCell);
                    }
                }
            }
        }

        // Add noise to a neighboring ramp, if any
        foreach (var mapCell in noise.ToList()) {
            var mapCellNeighbors = mapCell.Position.ToVector2().GetNeighbors();
            var rampToAddTo = ramps.FirstOrDefault(ramp => mapCellNeighbors.Any(ramp.Contains));
            if (rampToAddTo != default) {
                rampToAddTo.Add(mapCell.Position.ToVector2());
                noise.Remove(mapCell);
            }
        }

        return (ramps, noise);
    }

    /// <summary>
    /// A real ramp connects two different height layers
    /// If all the tiles in the given ramp are roughly on the same height, this is not a ramp
    /// </summary>
    /// <param name="rampCluster"></param>
    /// <returns>True if the tiles have varied heights, false otherwise</returns>
    private bool IsReallyARamp(IReadOnlyCollection<MapCell> rampCluster) {
        // This fixes some glitches
        if (rampCluster.Count < 7) {
            return false;
        }

        var minHeight = rampCluster.Min(cell => _mapAnalyzer.WithWorldHeight(cell.Position).Z);
        var maxHeight = rampCluster.Max(cell => _mapAnalyzer.WithWorldHeight(cell.Position).Z);
        var heightDifference = Math.Abs(minHeight - maxHeight);

        return 0.05 < heightDifference && heightDifference < 10;
    }

    private List<ChokePoint> ComputePotentialChokePoints() {
        return _rayCastingChokeFinder.FindChokePoints();
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
            var chokePointCombinations = MathUtils.Combinations(chokesInRegion, nbChokesToConsider).Select(setOfChokes => setOfChokes.ToList());
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
        var floodFill = Clustering.Instance.FloodFill(subregion, subregion.First());
        return floodFill.Count() == subregion.Count;
    }

    private static Dictionary<Vector2, Region> BuildRegionsLookupMap(List<Region> regions) {
        var regionsMap = new Dictionary<Vector2, Region>();
        foreach (var region in regions) {
            foreach (var cell in region.Cells) {
                regionsMap[cell] = region;
            }
        }

        return regionsMap;
    }
}
