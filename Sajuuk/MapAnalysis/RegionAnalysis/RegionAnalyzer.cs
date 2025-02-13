﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Sajuuk.Algorithms;
using Sajuuk.GameData;
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
    private readonly IUnitsTracker _unitsTracker;
    private readonly FootprintCalculator _footprintCalculator;
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
        IUnitsTracker unitsTracker,
        FootprintCalculator footprintCalculator,
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
        _unitsTracker = unitsTracker;
        _footprintCalculator = footprintCalculator;
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

        var cellsToConsider = _terrainTracker.PlayableCells;
        Logger.Info($"Starting region analysis on {cellsToConsider.Count} cells ({_terrainTracker.MaxX}x{_terrainTracker.MaxY})");

        var ramps = _rampFinder.FindRamps(cellsToConsider);
        var rampsMapImage = _mapImageFactory.CreateMapImage();
        foreach (var rampCell in ramps.SelectMany(ramp => ramp.Cells)) {
            rampsMapImage.SetCellColor(rampCell, Color.Teal);
        }
        rampsMapImage.Save(FileNameFormatter.FormatDataFileName("Ramps", _mapFileName, "png"));

        var chokePoints = _chokeFinder.FindChokePoints();
        var regions = FindRegions(cellsToConsider, ramps, chokePoints);
        var noise = cellsToConsider.Except(regions.SelectMany(region => region.Cells));

        _regionsData = new RegionsData(regions.Select(region => region as Region).ToList(), ramps, noise, chokePoints);
        _regionsRepository.Save(_regionsData, gameInfo.MapName);

        var nbRegions = _regionsData.Regions.Count;
        var nbObstructed = _regionsData.Regions.Count(region => region.IsObstructed);
        var nbRamps = _regionsData.Ramps.Count;
        var nbNoise = _regionsData.Noise.Count;
        var nbChokePoints = _regionsData.ChokePoints.Count;
        Logger.Metric($"{nbRegions} regions ({nbObstructed} obstructed), {nbRamps} ramps, {nbNoise} unclassified cells and {nbChokePoints} choke points");
        DebugReachableNeighbors();

        Logger.Success("Region analysis done and saved");
    }

    /// <summary>
    /// Decomposes the given cells into regions, taking into account the ramps and potential choke points.
    /// The returned regions should be deterministic, as in building the same regions should provide regions with the same ids, colors, cell order, etc.
    /// </summary>
    /// <param name="cellsToConsider">The cells to build regions from.</param>
    /// <param name="ramps">The ramps on the map.</param>
    /// <param name="potentialChokePoints">The potential choke points on the map.</param>
    /// <returns>The detected regions.</returns>
    private List<AnalyzedRegion> FindRegions(IReadOnlyCollection<Vector2> cellsToConsider, IReadOnlyCollection<Ramp> ramps, IReadOnlyCollection<ChokePoint> potentialChokePoints) {
        Logger.Info($"Finding regions with {cellsToConsider.Count} cellsToConsider, {ramps.Count} ramps and {potentialChokePoints.Count} potentialChokePoints");
        var regions = ComputeObstaclesRegions(cellsToConsider, ramps);
        Logger.Info($"{regions.Count} obstacle regions");

        var cellsToConsiderForRegionSplit = cellsToConsider
            .Except(ramps.SelectMany(ramp => ramp.Cells))
            .Except(regions.SelectMany(region => region.Cells))
            .ToList();
        Logger.Info($"{cellsToConsiderForRegionSplit.Count} cellsToConsiderForRegionSplit");

        var regionsPotentialCells = ComputePotentialRegionCells(cellsToConsiderForRegionSplit).ToList();
        Logger.Info($"{regionsPotentialCells.Count} regionsPotentialCells");
        var potentialRegions = ComputePotentialRegions(regionsPotentialCells);
        Logger.Info($"{potentialRegions.Count} potentialRegions");

        foreach (var region in potentialRegions) {
            var subregions = BreakDownIntoSubregions(region.ToHashSet(), potentialChokePoints, saveSplitsAsImage: false);
            Logger.Info($"{subregions.Count} subregions");
            regions.AddRange(subregions.Select(subregion => _regionFactory.CreateAnalyzedRegion(subregion, RegionType.Unknown, _expandAnalyzer.ExpandLocations)));
        }

        regions.AddRange(ramps.Select(ramp => _regionFactory.CreateAnalyzedRegion(ramp.Cells, RegionType.Ramp, _expandAnalyzer.ExpandLocations)));

        // We order the regions to have a deterministic regions order.
        // It'll also make it easier to find a region by its id because they're going to be left to right, bottom to top.
        regions = regions
            .OrderBy(region => region.Center.Y)
            .ThenBy(region => region.Center.X)
            .ToList();

        for (var regionId = 0; regionId < regions.Count; regionId++) {
            regions[regionId].FinalizeCreation(regionId, regions);
        }

        return regions;
    }

    /// <summary>
    /// Some obstacles create barriers between future regions.
    /// We'll create dedicated regions for these obstacles.
    /// It will make it easier to determine unreachable regions later on.
    /// </summary>
    /// <param name="cellsToConsider">The cells to find regions from.</param>
    /// <param name="ramps">The ramps on the map.</param>
    /// <returns>A list of regions made from obstacles.</returns>
    private List<AnalyzedRegion> ComputeObstaclesRegions(IReadOnlyCollection<Vector2> cellsToConsider, IReadOnlyCollection<Ramp> ramps) {
        var allRampsCells = ramps.SelectMany(ramp => ramp.Cells).ToHashSet();
        Logger.Info($"{allRampsCells.Count} allRampsCells");
        var obstacleGroups = ComputeObstacleGroups(cellsToConsider.Except(allRampsCells));
        Logger.Info($"{obstacleGroups.Count} obstacleGroups");

        var regions = new List<AnalyzedRegion>();
        foreach (var obstacleGroup in obstacleGroups) {
            var borderingCells = ComputeBorderingCells(obstacleGroup.ToHashSet()).ToList();
            Logger.Info($"{borderingCells.Count} borderingCells");
            var borderingCellsGroups = borderingCells.GroupBy(cell => _terrainTracker.IsWalkable(cell, considerObstaclesObstructions: false));
            Logger.Info($"{borderingCellsGroups.Count()} borderingCellsGroups");

            var nbClusters = borderingCellsGroups
                .Select(group => _clustering.DBSCAN(group.ToList(), epsilon: (float)Math.Sqrt(2), minPoints: 1).clusters)
                .Select(clusters => clusters.Count)
                .Sum();
            Logger.Info($"{nbClusters} nbClusters");

            // 4 or more clusters means we have 2 sides of the obstacle that are walkable, and two sides that are unwalkable.
            if (nbClusters >= 4) {
                regions.Add(_regionFactory.CreateAnalyzedRegion(obstacleGroup, RegionType.OpenArea, _expandAnalyzer.ExpandLocations));
            }
        }

        return regions;
    }

    /// <summary>
    /// Groups adjacent obstacles together and returns the set of their footprints.
    /// </summary>
    /// <param name="cellsToConsider">The cells to find obstacles in.</param>
    /// <returns>The list of all obstacle groups.</returns>
    private List<List<Vector2>> ComputeObstacleGroups(IEnumerable<Vector2> cellsToConsider) {
        Logger.Info($"ComputeObstacleGroups with {cellsToConsider.Count()} cellsToConsider");
        var cellToConsiderMapImage = _mapImageFactory.CreateMapImage();
        foreach (var cell in cellsToConsider) {
            cellToConsiderMapImage.SetCellColor(cell, Color.MediumPurple);
        }
        cellToConsiderMapImage.Save(FileNameFormatter.FormatDataFileName($"ObstacleGroups_Cells", _mapFileName, "png"));

        var units = _unitsTracker.GetUnits(_unitsTracker.NeutralUnits, Units.Obstacles.Concat(Units.MineralFields).ToHashSet()).ToList();
        Logger.Info($"{units.Count} units");
        var footprints = units.SelectMany(_footprintCalculator.GetFootprint).ToList();
        Logger.Info($"{footprints.Count} footprints");
        var cells = footprints.Where(cellsToConsider.Contains).ToList();
        Logger.Info($"{cells.Count} obstacle group cells");

        var footprintsMapImage = _mapImageFactory.CreateMapImage();
        foreach (var cell in footprints) {
            footprintsMapImage.SetCellColor(cell, Color.Red);
        }

        foreach (var cell in cells) {
            footprintsMapImage.SetCellColor(cell, Color.Cyan);
        }
        footprintsMapImage.Save(FileNameFormatter.FormatDataFileName($"ObstacleGroups_Footprints", _mapFileName, "png"));

        var clusters = _clustering.DBSCAN(cells, epsilon: (float)Math.Sqrt(2), minPoints: 1).clusters;

        var clustersMapImage = _mapImageFactory.CreateMapImage();
        foreach (var cell in clusters.SelectMany(cluster => cluster)) {
            clustersMapImage.SetCellColor(cell, Color.MediumPurple);
        }
        clustersMapImage.Save(FileNameFormatter.FormatDataFileName($"ObstacleGroups_Clusters", _mapFileName, "png"));

        return clusters;
    }

    /// <summary>
    /// Computes the cells that outline the given cellGroup.
    /// </summary>
    /// <param name="cellGroup">The cells to get the bordering cells of.</param>
    /// <returns>All cells that touch a cell in the given cell group.</returns>
    private static IEnumerable<Vector2> ComputeBorderingCells(IReadOnlySet<Vector2> cellGroup) {
        return cellGroup
            .SelectMany(cell => cell.GetNeighbors())
            .Where(neighbor => !cellGroup.Contains(neighbor));
    }

    /// <summary>
    /// Computes the potential region cells.
    /// This computation will consider cells that have no reachable neighbors (isolated cells) as noise.
    /// </summary>
    /// <param name="cellsToConsider">The cells to decompose into regions.</param>
    /// <returns>The region cells and the cells that are considered noise.</returns>
    private IEnumerable<Vector2> ComputePotentialRegionCells(IEnumerable<Vector2> cellsToConsider) {
        var potentialRegionCells = cellsToConsider.ToHashSet();

        // Some cells have 0 reachable neighbors, which messes up the use of FloodFill.
        // DragonScalesAIE has 2 cells like that.
        // This would probably not be the case after fixing https://github.com/Guillaume-Docquier/Sajuuk-SC2/issues/43
        var isolatedCells = potentialRegionCells
            .Where(cell => !_terrainTracker.GetReachableNeighbors(cell, considerObstaclesObstructions: false).Any())
            .ToList();
        Logger.Info($"{isolatedCells.Count} isolatedCells");

        return potentialRegionCells.Except(isolatedCells);
    }

    /// <summary>
    /// <para>Computes the potential regions by using clustering and highly penalizing height differences.</para>
    /// <para>This penalty will allow us to isolate ramps by turning them into noise, which we can cluster properly afterwards.</para>
    /// <para>We will later on try to break down each region into sub regions, because not all regions are separated by ramps.</para>
    /// </summary>
    /// <returns>
    /// The potential regions and the cells that are not part of any region.
    /// </returns>
    private List<HashSet<Vector2>> ComputePotentialRegions(IEnumerable<Vector2> regionCells) {
        // TODO Not sure this is needed since we already computed ramps and excluded them from the given regionCells
        var regionCells3d = regionCells.Select(cell => {
            // Highly penalize height differences during clustering
            var trickPosition = _terrainTracker.WithWorldHeight(cell);
            trickPosition.Z *= RegionZMultiplier;

            return trickPosition;
        }).ToList();

        var clusteringResult = _clustering.DBSCAN(regionCells3d, epsilon: _diagonalDistance + 0.04f, minPoints: 6);

        var potentialRegions = clusteringResult.clusters
            .Select(cluster => cluster.Select(cell => cell.ToVector2()).ToHashSet())
            .ToList();

        // Add noise to any neighboring region
        var noise = clusteringResult.noise.Select(noise => noise.ToVector2()).ToList();
        foreach (var noisyCell in noise) {
            var mapCellNeighbors = noisyCell.GetNeighbors();
            var regionToAddTo = potentialRegions.FirstOrDefault(potentialRegion => mapCellNeighbors.Any(potentialRegion.Contains));
            regionToAddTo?.Add(noisyCell);
        }

        return potentialRegions;
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
        Logger.Info($"{chokesInRegion.Count} chokesInRegion");

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

    /// <summary>
    /// Prints information about the regions reachable neighbors.
    /// We also save an image with red lines in between unreachable neighbors.
    /// </summary>
    private void DebugReachableNeighbors() {
        var mapImage = _mapImageFactory.CreateMapImage();

        // Draw obstructions (rocks, minerals)
        foreach (var cell in _terrainTracker.ObstructedCells) {
            mapImage.SetCellColor(cell, Color.Teal);
        }

        foreach (var region in Regions) {
            var neighbors = region.Neighbors.Select(neighbor => neighbor.Region);
            var reachableNeighbors = region.GetReachableNeighbors();
            var unreachableNeighbors = neighbors.Except(reachableNeighbors).ToList();

            // Draw unreachable neighbors
            foreach (var unreachableNeighbor in unreachableNeighbors) {
                foreach (var cell in region.Center.GetPointsInBetween(unreachableNeighbor.Center)) {
                    mapImage.SetCellColor(cell, Color.Red);
                }

                mapImage.SetCellColor(region.Center, Color.MediumBlue);
                mapImage.SetCellColor(unreachableNeighbor.Center, Color.MediumBlue);
            }
        }

        mapImage.Save(FileNameFormatter.FormatDataFileName("UnreachableNeighbors", _mapFileName, "png"));
    }
}
