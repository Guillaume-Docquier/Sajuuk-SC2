using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Sajuuk.Algorithms;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis.ExpandAnalysis;
using Sajuuk.MapAnalysis.RegionAnalysis.ChokePoints;
using Sajuuk.MapAnalysis.RegionAnalysis.Ramps;
using Sajuuk.Persistence;
using Sajuuk.Utils;
using SC2APIProtocol;
using Color = System.Drawing.Color;

namespace Sajuuk.MapAnalysis.RegionAnalysis;

public class RegionAnalyzer : IRegionAnalyzer, INeedUpdating {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IExpandAnalyzer _expandAnalyzer;
    private readonly IClustering _clustering;
    private readonly IMapDataRepository<RegionsData> _regionsRepository;
    private readonly IChokeFinder _chokeFinder;
    private readonly IRampFinder _rampFinder;
    private readonly IRegionFactory _regionFactory;
    private readonly IMapImageFactory _mapImageFactory;
    private readonly string _mapFileName;

    private const float RegionZMultiplier = 8;
    private readonly float _diagonalDistance = (float)Math.Sqrt(2);

    /// <summary>
    /// The smallest ramps are pretty small (14 cells), let's not make regions smaller than that.
    /// Increased for better results.
    /// </summary>
    private const int MinRegionSize = 16;

    private RegionsData _regionsData;

    public bool IsAnalysisComplete => _regionsData != null;
    public List<IRegion> Regions => _regionsData.Regions.Select(region => region as IRegion).ToList();

    public RegionAnalyzer(
        ITerrainTracker terrainTracker,
        IExpandAnalyzer expandAnalyzer,
        IClustering clustering,
        IMapDataRepository<RegionsData> regionsRepository,
        IChokeFinder chokeFinder,
        IRampFinder rampFinder,
        IRegionFactory regionFactory,
        IMapImageFactory mapImageFactory,
        string mapFileName
    ) {
        _terrainTracker = terrainTracker;
        _expandAnalyzer = expandAnalyzer;
        _clustering = clustering;
        _regionsRepository = regionsRepository;
        _chokeFinder = chokeFinder;
        _rampFinder = rampFinder;
        _regionFactory = regionFactory;
        _mapImageFactory = mapImageFactory;
        _mapFileName = mapFileName;
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

        Logger.Info($"Starting region analysis on {_terrainTracker.PlayableCells.Count} cells ({_terrainTracker.MaxX}x{_terrainTracker.MaxY})");

        var ramps = _rampFinder.FindRamps(_terrainTracker.PlayableCells);

        var allRampCells = ramps.SelectMany(ramp => ramp.Cells);
        var (regionsPotentialCells, potentialRegionCellsNoise) = ComputePotentialRegionCells(_terrainTracker.PlayableCells.Except(allRampCells));
        var (potentialRegions, regionNoise) = ComputePotentialRegions(regionsPotentialCells);

        var chokePoints = _chokeFinder.FindChokePoints();

        // We order the regions to have a deterministic regions order.
        // It'll also make it easier to find a region by its id because they're going to be left to right, bottom to top.
        var regions = BuildRegions(potentialRegions, ramps, chokePoints)
            .OrderBy(region => region.Center.Y)
            .ThenBy(region => region.Center.X)
            .ToList();

        for (var regionId = 0; regionId < regions.Count; regionId++) {
            regions[regionId].FinalizeCreation(regionId, regions);
        }

        var noise = regionNoise
            .Concat(potentialRegionCellsNoise)
            .ToList();

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
    /// Computes the potential region cells.
    /// This computation will consider cells that have no reachable neighbors (isolated cells) as noise.
    /// </summary>
    /// <param name="cellsToConsider">The cells to decompose into regions.</param>
    /// <returns>The region cells and the cells that are considered noise.</returns>
    private (List<Vector2> potentialRegionCells, List<Vector2> potentialRegionCellsNoise) ComputePotentialRegionCells(IEnumerable<Vector2> cellsToConsider) {
        var potentialRegionCells = cellsToConsider.ToHashSet();

        // Some cells have 0 reachable neighbors, which messes up the use of FloodFill.
        // DragonScalesAIE has 2 cells like that.
        // This would probably not be the case after fixing https://github.com/Guillaume-Docquier/Sajuuk-SC2/issues/43
        var isolatedCells = potentialRegionCells
            .Where(cell => !_terrainTracker.GetReachableNeighbors(cell, considerObstaclesObstructions: false).Any())
            .ToList();

        return (potentialRegionCells.Except(isolatedCells).ToList(), isolatedCells);
    }

    /// <summary>
    /// <para>Computes the potential regions by using clustering and highly penalizing height differences.</para>
    /// <para>This penalty will allow us to isolate ramps by turning them into noise, which we can cluster properly afterwards.</para>
    /// <para>We will later on try to break down each region into sub regions, because not all regions are separated by ramps.</para>
    /// </summary>
    /// <returns>
    /// The potential regions and the cells that are not part of any region.
    /// </returns>
    private (List<HashSet<Vector2>> potentialRegions, IEnumerable<Vector2> regionsNoise) ComputePotentialRegions(IEnumerable<Vector2> regionCells) {
        // TODO Not sure this is needed since we already computed ramps and excluded them from the given regionCells
        var regionCells3d = regionCells.Select(cell => {
            // Highly penalize height differences during clustering
            var trickPosition = _terrainTracker.WithWorldHeight(cell);
            trickPosition.Z *= RegionZMultiplier;

            return trickPosition;
        }).ToList();

        var clusteringResult = _clustering.DBSCAN(regionCells3d, epsilon: _diagonalDistance + 0.04f, minPoints: 6);
        var noise = clusteringResult.noise.Select(noise => noise.ToVector2()).ToHashSet();

        var potentialRegions = clusteringResult.clusters
            .Select(cluster => cluster.Select(cell => cell.ToVector2()).ToHashSet())
            .ToList();

        // Add noise to any neighboring region
        foreach (var noisyCell in noise.ToList()) {
            var mapCellNeighbors = noisyCell.GetNeighbors();
            var regionToAddTo = potentialRegions.FirstOrDefault(potentialRegion => mapCellNeighbors.Any(potentialRegion.Contains));
            if (regionToAddTo != default) {
                regionToAddTo.Add(noisyCell);
                noise.Remove(noisyCell);
            }
        }

        return (potentialRegions, noise);
    }

    private List<AnalyzedRegion> BuildRegions(List<HashSet<Vector2>> potentialRegions, List<Ramp> ramps, List<ChokePoint> potentialChokePoints) {
        var regions = new List<AnalyzedRegion>();
        foreach (var region in potentialRegions) {
            var subregions = BreakDownIntoSubregions(region.ToHashSet(), potentialChokePoints, saveSplitsAsImage: false);
            regions.AddRange(subregions.Select(subregion => _regionFactory.CreateAnalyzedRegion(subregion, RegionType.Unknown, _expandAnalyzer.ExpandLocations)));
        }

        regions.AddRange(ramps.Select(ramp => _regionFactory.CreateAnalyzedRegion(ramp.Cells, RegionType.Ramp, _expandAnalyzer.ExpandLocations)));

        return regions;
    }

    /// <summary>
    /// <para>Break down a given region into subregions based on choke points</para>
    /// <para>We might use more than one choke point to break down a single region</para>
    /// <para>Regions that are broken down must be big enough to be considered a valid split</para>
    /// </summary>
    /// <param name="region">The region to break down into 2 sub regions.</param>
    /// <param name="potentialChokePoints">The choke points to use for the split.</param>
    /// <param name="saveSplitsAsImage">Whether to save the splits as images for debugging.</param>
    /// <returns>A list of subregions created by splitting the region with the choke points.</returns>
    private List<List<Vector2>> BreakDownIntoSubregions(IReadOnlySet<Vector2> region, IReadOnlyCollection<ChokePoint> potentialChokePoints, bool saveSplitsAsImage) {
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
            var chokePointCombinations = MathUtils.Combinations(chokesInRegion, nbChokesToConsider)
                .Select(setOfChokes => setOfChokes.ToList())
                .ToList();

            if (chokePointCombinations.Count > 20_000) {
                Logger.Warning($"We are about to try {chokePointCombinations.Count} choke point combinations ({nbChokesToConsider} from {chokesInRegion.Count}). There is most likely an issue.");
            }

            foreach (var chokePointCombination in chokePointCombinations) {
                var (subregion1, subregion2) = SplitRegion(region, chokePointCombination.SelectMany(choke => choke.Edge).ToList());
                var maxChokeLength = chokePointCombination.Max(choke => choke.Edge.Count);

                if (saveSplitsAsImage) {
                    SaveSplitAsImage(region, chokePointCombination, subregion1, subregion2, maxChokeLength);
                }

                if (IsValidSplit(subregion1, maxChokeLength) && IsValidSplit(subregion2, maxChokeLength)) {
                    var unusedChokePoints = potentialChokePoints
                        .Except(chokePointCombination)
                        .ToList();

                    return BreakDownIntoSubregions(subregion1, unusedChokePoints, saveSplitsAsImage)
                        .Concat(BreakDownIntoSubregions(subregion2, unusedChokePoints, saveSplitsAsImage))
                        .ToList();
                }
            }

            nbChokesToConsider++;
        }

        return new List<List<Vector2>> { region.ToList() };
    }

    /// <summary>
    /// Splits a region into 2 subregions using the given set of separation cells.
    /// This can produce an empty subregion if the separation cells do not cut the region into two parts.
    /// </summary>
    /// <param name="regionToSplit">The region to split.</param>
    /// <param name="separations">The cells to use to split the region.</param>
    /// <returns>The two subregions.</returns>
    private (HashSet<Vector2> subregion1, HashSet<Vector2> subregion2) SplitRegion(IReadOnlySet<Vector2> regionToSplit, IReadOnlyCollection<Vector2> separations) {
        var startingPoint = regionToSplit.First(point => !separations.Contains(point));
        var subregion1 = _clustering.FloodFill(regionToSplit.Except(separations).ToHashSet(), startingPoint).ToHashSet();
        var subregion2 = regionToSplit.Except(subregion1).ToHashSet();

        return (subregion1, subregion2);
    }

    /// <summary>
    /// Determines if a subregion is the result of a valid split given the cut length.
    /// A subregion should be a single cluster of cells that's big enough compared to the cut.
    /// </summary>
    /// <param name="subregion">The subregion to validate.</param>
    /// <param name="cutLength">The length of the cut used to create this region.</param>
    /// <returns>True if the split is valid, false otherwise.</returns>
    private bool IsValidSplit(IReadOnlyCollection<Vector2> subregion, float cutLength) {
        if (subregion.Count < MinRegionSize) {
            return false;
        }

        // If the split region is too small compared to the cut, it might not be worth a cut
        if (subregion.Count <= Math.Max(10, cutLength * cutLength / 2)) {
            return false;
        }

        // A region should form a single cluster of cells
        var floodFill = _clustering.FloodFill(subregion.ToHashSet(), subregion.First());
        return floodFill.Count() == subregion.Count;
    }

    /// <summary>
    /// A debugging function that will produce a png image of a region split.
    /// </summary>
    /// <param name="region">The region that was split.</param>
    /// <param name="chokePointCombination">The choke points used to split the region.</param>
    /// <param name="subregion1">The first subregion.</param>
    /// <param name="subregion2">The second subregion.</param>
    /// <param name="cutLength">The cut length, for split validation.</param>
    private void SaveSplitAsImage(IEnumerable<Vector2> region, IEnumerable<ChokePoint> chokePointCombination, HashSet<Vector2> subregion1, HashSet<Vector2> subregion2, int cutLength) {
        var mapImage = _mapImageFactory.CreateMapImage();
        foreach (var cell in region) {
            mapImage.SetCellColor(cell, Color.Cyan);
        }

        var isSubRegion1Valid = IsValidSplit(subregion1, cutLength);
        var subregion1Color = isSubRegion1Valid ? Color.Plum : Color.Purple;
        foreach (var cell in subregion1) {
            mapImage.SetCellColor(cell, subregion1Color);
        }

        var isSubRegion2Valid = IsValidSplit(subregion2, cutLength);
        var subregion2Color = isSubRegion2Valid ? Color.RoyalBlue : Color.MediumBlue;
        foreach (var cell in subregion2) {
            mapImage.SetCellColor(cell, subregion2Color);
        }

        // We paint the choke point last otherwise we wouldn't see it because it is included in the subregions.
        var isValidSplit = isSubRegion1Valid && isSubRegion2Valid;
        var splitColor = isValidSplit ? Color.Lime : Color.Red;
        foreach (var cell in chokePointCombination.SelectMany(chokePoint => chokePoint.Edge)) {
            mapImage.SetCellColor(cell, splitColor);
        }

        mapImage.Save(FileNameFormatter.FormatDataFileName($"RegionSplit_{DateTime.UtcNow.Ticks}", _mapFileName, "png"));
    }
}
