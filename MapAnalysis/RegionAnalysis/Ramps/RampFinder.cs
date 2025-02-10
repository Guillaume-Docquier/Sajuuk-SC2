using System.Drawing;
using System.Numerics;
using Algorithms;
using Algorithms.ExtensionMethods;
using MapAnalysis.RegionAnalysis.Persistence;
using SC2Client;
using SC2Client.Debugging.Images;
using SC2Client.ExtensionMethods;
using SC2Client.Trackers;

namespace MapAnalysis.RegionAnalysis.Ramps;

/// <summary>
/// Debugging notes:
/// - Berlingrad:
///   - Vision blockers (unbuildable)
///   - Ramps that touch
/// </summary>
public class RampFinder : IRampFinder {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IMapImageFactory _mapImageFactory;
    private readonly IMapFileNameFormatter _mapFileNameFormatter;
    private readonly ILogger _logger;
    private readonly string _mapFileName;

    /// <summary>
    /// Some testing shows that groups of cells either cover less than 1% of the map (ramp), or more than 10% (region).
    /// We'll use 3% to give us some leeway.
    /// </summary>
    private const float RegionCoverageThreshold = 0.03f;
    private readonly float _diagonalDistance = (float)Math.Sqrt(2);

    public RampFinder(
        ITerrainTracker terrainTracker,
        IMapImageFactory mapImageFactory,
        IMapFileNameFormatter mapFileNameFormatter,
        ILogger logger,
        string mapFileName
    ) {
        _terrainTracker = terrainTracker;
        _mapImageFactory = mapImageFactory;
        _mapFileNameFormatter = mapFileNameFormatter;
        _logger = logger;
        _mapFileName = mapFileName;
    }

    /// <summary>
    /// Identify ramps within the given cells.
    /// This is done by clustering cells that are not buildable.
    /// Not all of these cells will be ramps, as some unbuildable cells are vision blockers.
    /// We filter out vision blockers by the properties of the cluster. That is, the amount of height variation and size of the cluster.
    /// </summary>
    /// <returns>The ramps that were found.</returns>
    public List<Ramp> FindRamps(IReadOnlyCollection<Vector2> cellsToConsider) {
        var potentialRampCells = ComputePotentialRampCells(cellsToConsider);
        // TODO GD We don't even need to check the heights, we can just get unbuildables, I think.
        // var potentialRampCells = cellsToConsider.Where(cell => !_terrainTracker.IsBuildable(cell, considerObstructions: false)).ToHashSet();

        // We cluster once for an initial split
        var initialRampSplit = Clustering.DBSCAN(potentialRampCells, epsilon: _diagonalDistance, minPoints: 1);
        var noise = initialRampSplit.noise.ToHashSet();

        var ramps = new List<Ramp>();
        foreach (var initialRampCluster in initialRampSplit.clusters) {
            var clusterSet = initialRampCluster.ToHashSet();
            var maxConnections = initialRampCluster.Max(cell => cell.GetNeighbors().Count(neighbor => clusterSet.Contains(neighbor)));
            if (maxConnections < 8) {
                // This is to make (small?) ramps work
                maxConnections = (int)Math.Floor(0.875f * maxConnections);
            }

            // Some ramps touch each other (berlingrad)
            // We do a 2nd round of clustering based on the connectivity of the cluster
            // This is because ramps have low connectivity, so we need it to be variable
            var rampClusterResult = Clustering.DBSCAN(initialRampCluster, epsilon: _diagonalDistance, minPoints: maxConnections);

            foreach (var mapCell in rampClusterResult.noise) {
                noise.Add(mapCell);
            }

            foreach (var rampCluster in rampClusterResult.clusters) {
                if (IsReallyARamp(rampCluster)) {
                    ramps.Add(new Ramp(rampCluster.ToHashSet()));
                }
                else {
                    foreach (var mapCell in rampCluster) {
                        noise.Add(mapCell);
                    }
                }
            }
        }

        // Add noise to a neighboring ramp, if any
        // This can be due to inaccuracies in the previous clustering cells, or height errors in the data
        var allRampCells = ramps.SelectMany(ramp => ramp.Cells).ToHashSet();
        var orderedNoise = noise.OrderBy(noisyCell => allRampCells.Min(rampCell => rampCell.DistanceTo(noisyCell))).ToList();
        foreach (var noisyCell in orderedNoise) {
            var noisyCellRampNeighbors = noisyCell.GetNeighbors();

            var rampToAddTo = ramps.FirstOrDefault(ramp => noisyCellRampNeighbors.Any(ramp.Cells.Contains));
            if (rampToAddTo != null) {
                rampToAddTo.Cells.Add(noisyCell);
                allRampCells.Add(noisyCell);
            }
        }

        _mapImageFactory
            .CreateMapImageWithTerrain()
            .SetCellsColor(orderedNoise, Color.Red)
            .SetCellsColor(allRampCells, Color.Teal)
            .Save(_mapFileNameFormatter.Format("PotentialRampCells", _mapFileName));

        return ramps;
    }

    /// <summary>
    /// SC2 maps have 2-3-4 layers of cells with different heights that will represent most of the map.
    /// Any other cell with a different height is most likely a ramp!
    /// Some testing shows that groups of cells either cover less than 1% of the map (ramp), or more than 10% (region).
    /// </summary>
    /// <param name="cellsToConsider"></param>
    /// <returns></returns>
    private HashSet<Vector2> ComputePotentialRampCells(IReadOnlyCollection<Vector2> cellsToConsider) {
        var potentialRampCells = new HashSet<Vector2>();
        var heightGroups = cellsToConsider.Select(cell => _terrainTracker.WithWorldHeight(cell)).GroupBy(c => c.Z);
        foreach (var heightGroup in heightGroups.OrderBy(g => g.Count())) {
            var percentOfMap = heightGroup.Count() / (float)cellsToConsider.Count;
            if (percentOfMap < RegionCoverageThreshold) {
                // Some non-ramp cells might fall into this category because they have the wrong height.
                // At this point, we know for sure that the cell must be unbuildable, so if it is buildable, we'll attribute it to a height error.
                var rampCells = heightGroup
                    .Select(cell => cell.ToVector2())
                    .Where(cell => !_terrainTracker.IsBuildable(cell, considerObstructions: false));

                foreach (var rampCell in rampCells) {
                    potentialRampCells.Add(rampCell);
                }
            }
        }

        // Some cells are on the regions cell layers but are still unbuildable, and thus should be ramps
        // However, vision blockers are unbuildable and on regions cell layers, so we have to exclude them, somehow
        // We will do this by adding candidates to potential ramps
        var extraCandidates = cellsToConsider.Where(cell => !_terrainTracker.IsBuildable(cell, considerObstructions: false)).ToHashSet();
        extraCandidates.ExceptWith(potentialRampCells);

        // We sort candidates by closest to ramps so we can add them via a single pass (hopefully)
        var orderedUnbuildableCells = extraCandidates
            .OrderBy(cell => potentialRampCells.Min(potentialRampCell => potentialRampCell.DistanceTo(cell)))
            .ToList();

        var nbPasses = -1;
        var cellsAdded = true;
        while (cellsAdded) {
            nbPasses++;
            cellsAdded = false;
            foreach (var orderedUnbuildableCell in orderedUnbuildableCells.ToList()) {
                if (potentialRampCells.Contains(orderedUnbuildableCell)) {
                    // Will happen if we do multiple passes because we won't remove from the list due to time inefficiency (we need it to be an ordered list)
                    continue;
                }

                var neighbors = orderedUnbuildableCell.GetNeighbors();
                if (neighbors.Any(potentialRampCells.Contains)) {
                    potentialRampCells.Add(orderedUnbuildableCell);
                    cellsAdded = true;
                }
            }
        }

        _logger.Debug($"Added all extra ramp candidates in {nbPasses} passes.");

        return potentialRampCells;
    }

    /// <summary>
    /// TODO GD Might not need this anymore
    /// A real ramp connects two different height layers with a progressive slope.
    /// If all the tiles in the given ramp are roughly on the same height, this is not a ramp.
    /// - It is probably a group of vision blockers, which are also walkable and unbuildable.
    /// </summary>
    /// <param name="rampCluster">The cells in a ramp</param>
    /// <returns>True if the tiles have varied heights that correspond to typical ramp characteristics, false otherwise</returns>
    private bool IsReallyARamp(IReadOnlyCollection<Vector2> rampCluster) {
        var nbDifferentHeights = rampCluster
            .Select(cell => _terrainTracker.WithWorldHeight(cell).Z)
            .ToHashSet()
            .Count;

        // Ramps typically have nbDifferentHeights = [4, 9]
        if (nbDifferentHeights < 4) {
            return false;
        }

        var minHeight = rampCluster.Min(cell => _terrainTracker.WithWorldHeight(cell).Z);
        var maxHeight = rampCluster.Max(cell => _terrainTracker.WithWorldHeight(cell).Z);
        var heightDifference = Math.Abs(minHeight - maxHeight);

        // Ramps typically have heightDifference = [1.5, 2]
        return heightDifference > 1f;
    }
}
