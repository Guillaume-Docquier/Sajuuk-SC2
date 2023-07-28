using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Sajuuk.Algorithms;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis.ExpandAnalysis;
using Sajuuk.MapAnalysis.RegionAnalysis.ChokePoints;
using Sajuuk.Persistence;
using Sajuuk.Utils;
using SC2APIProtocol;

namespace Sajuuk.MapAnalysis.RegionAnalysis;

public class RegionAnalyzer : IRegionAnalyzer, INeedUpdating {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IExpandAnalyzer _expandAnalyzer;
    private readonly IClustering _clustering;
    private readonly IPathfinder _pathfinder;
    private readonly IMapDataRepository<RegionsData> _regionsRepository;
    private readonly IChokeFinder _chokeFinder;

    private const int RegionMinPoints = 6;
    private const float RegionZMultiplier = 8;
    private readonly float _diagonalDistance = (float)Math.Sqrt(2);

    private RegionsData _regionsData;

    public bool IsAnalysisComplete => _regionsData != null;
    public List<IRegion> Regions => _regionsData.Regions.Select(region => region as IRegion).ToList();

    public RegionAnalyzer(
        ITerrainTracker terrainTracker,
        IExpandAnalyzer expandAnalyzer,
        IClustering clustering,
        IPathfinder pathfinder,
        IMapDataRepository<RegionsData> regionsRepository,
        IChokeFinder chokeFinder
    ) {
        _terrainTracker = terrainTracker;
        _expandAnalyzer = expandAnalyzer;
        _clustering = clustering;
        _pathfinder = pathfinder;
        _regionsRepository = regionsRepository;
        _chokeFinder = chokeFinder;
    }

    /// <summary>
    /// <para>Analyzes the map to find ramps and do region decomposition</para>
    /// <para>There should be at least 1 region per expand location and regions are always separated by ramps or choke points.</para>
    /// </summary>
    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        if (!_expandAnalyzer.IsAnalysisComplete) {
            return;
        }

        if (IsAnalysisComplete) {
            return;
        }

        var walkableMap = GenerateWalkableMap();
        Logger.Info($"Starting region analysis on {walkableMap.Count} cells ({_terrainTracker.MaxX}x{_terrainTracker.MaxY})");

        var rampsPotentialCells = walkableMap.Where(cell => !_terrainTracker.IsBuildable(cell.Position, includeObstacles: false)).ToList();
        var (ramps, rampsNoise) = ComputeRamps(rampsPotentialCells);

        var regionsPotentialCells = walkableMap
            .Where(cell => _terrainTracker.IsBuildable(cell.Position, includeObstacles: false))
            .Concat(rampsNoise)
            .ToList();
        var (potentialRegions, regionNoise) = ComputePotentialRegions(regionsPotentialCells);

        var noise = regionNoise.Select(mapCell => mapCell.Position.ToVector2()).ToList();
        var chokePoints = ComputePotentialChokePoints();

        // We order the regions to have a deterministic regions order.
        // It'll also make it easier to find a region by its id because they're going to be left to right, bottom to top.
        var regions = BuildRegions(potentialRegions, ramps, chokePoints)
            .OrderBy(region => region.Center.Y)
            .ThenBy(region => region.Center.X)
            .ToList();

        for (var regionId = 0; regionId < regions.Count; regionId++) {
            regions[regionId].FinalizeCreation(regionId, regions, _expandAnalyzer.ExpandLocations);
        }

        _regionsData = new RegionsData(regions.Select(region => region as Region).ToList(), ramps, noise, chokePoints);
        _regionsRepository.Save(_regionsData, gameInfo.MapName);

        var nbRegions = _regionsData.Regions.Count;
        var nbObstructed = _regionsData.Regions.Count(region => region.IsObstructed);
        var nbRamps = _regionsData.Ramps.Count;
        var nbNoise = _regionsData.Noise.Count;
        var nbChokePoints = _regionsData.ChokePoints.Count;
        Logger.Metric($"{nbRegions} regions ({nbObstructed} obstructed), {nbRamps} ramps, {nbNoise} unclassified cells and {nbChokePoints} choke points");
        Logger.Success("Region analysis done and saved");
    }

    /// <summary>
    /// Generates a list of MapCell representing each playable tile in the map.
    /// </summary>
    private List<MapCell> GenerateWalkableMap() {
        var map = new List<MapCell>();
        for (var x = 0; x < _terrainTracker.MaxX; x++) {
            for (var y = 0; y < _terrainTracker.MaxY; y++) {
                var mapCell = new MapCell(_terrainTracker.WithWorldHeight(new Vector2(x, y)).AsWorldGridCenter());
                if (_terrainTracker.IsWalkable(mapCell.Position, includeObstacles: false)) {
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
            var trickPosition = _terrainTracker.WithWorldHeight(mapCell.Position);
            trickPosition.Z *= RegionZMultiplier;

            mapCell.Position = trickPosition;
        });

        var noise = new HashSet<MapCell>();
        var clusteringResult = _clustering.DBSCAN(cells, epsilon: _diagonalDistance + 0.04f, minPoints: RegionMinPoints);
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
        var weakClusteringResult = _clustering.DBSCAN(cells, epsilon: 1, minPoints: 1);
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
            var rampClusterResult = _clustering.DBSCAN(weakCluster, epsilon: _diagonalDistance, minPoints: maxConnections);

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

        var minHeight = rampCluster.Min(cell => _terrainTracker.WithWorldHeight(cell.Position).Z);
        var maxHeight = rampCluster.Max(cell => _terrainTracker.WithWorldHeight(cell.Position).Z);
        var heightDifference = Math.Abs(minHeight - maxHeight);

        return 0.05 < heightDifference && heightDifference < 10;
    }

    private List<ChokePoint> ComputePotentialChokePoints() {
        return _chokeFinder.FindChokePoints();
    }

    private List<AnalyzedRegion> BuildRegions(List<HashSet<Vector2>> potentialRegions, List<HashSet<Vector2>> ramps, List<ChokePoint> potentialChokePoints) {
        var regions = new List<AnalyzedRegion>();
        foreach (var region in potentialRegions) {
            var subregions = BreakDownIntoSubregions(region.ToHashSet(), potentialChokePoints);
            regions.AddRange(subregions.Select(subregion => new AnalyzedRegion(_terrainTracker, _clustering, _pathfinder, subregion, RegionType.Unknown, _expandAnalyzer.ExpandLocations)));
        }

        regions.AddRange(ramps.Select(ramp => new AnalyzedRegion(_terrainTracker, _clustering, _pathfinder, ramp, RegionType.Ramp, _expandAnalyzer.ExpandLocations)));

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
    private List<List<Vector2>> BreakDownIntoSubregions(IReadOnlySet<Vector2> region, List<ChokePoint> potentialChokePoints) {
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
    private bool IsValidSplit(IReadOnlyCollection<Vector2> subregion, float cutLength) {
        // If the split region is too small compared to the cut, it might not be worth a cut
        if (subregion.Count <= Math.Max(10, cutLength * cutLength / 2)) {
            return false;
        }

        // A region should form a single cluster of cells
        var floodFill = _clustering.FloodFill(subregion, subregion.First());
        return floodFill.Count() == subregion.Count;
    }
}
