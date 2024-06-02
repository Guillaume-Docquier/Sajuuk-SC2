using System.Numerics;

namespace MapAnalysis.RegionAnalysis.Ramps;

public class RampFinder : IRampFinder {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IClustering _clustering;

    private readonly float _diagonalDistance = (float)Math.Sqrt(2);

    public RampFinder(ITerrainTracker terrainTracker, IClustering clustering) {
        _terrainTracker = terrainTracker;
        _clustering = clustering;
    }

    /// <summary>
    /// Identify ramps within the given cells.
    /// This is done by clustering cells that are walkable, but not buildable.
    /// Not all of these cells will be ramps, as some walkable but unbuildable cells are vision blockers.
    /// We filter out vision blockers by the properties of the cluster. That is, the amount of height variation and size of the cluster.
    /// </summary>
    /// <returns>The ramps that were found.</returns>
    public List<Ramp> FindRamps(IEnumerable<Vector2> cellsToConsider) {
        var potentialRampCells = cellsToConsider
            .Where(cell => _terrainTracker.IsWalkable(cell, considerObstaclesObstructions: false))
            .Where(cell => !_terrainTracker.IsBuildable(cell, considerObstaclesObstructions: false))
            .ToList();

        // We cluster once for an initial split
        var initialRampSplit = _clustering.DBSCAN(potentialRampCells, epsilon: 1, minPoints: 1);
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
            var rampClusterResult = _clustering.DBSCAN(initialRampCluster, epsilon: _diagonalDistance, minPoints: maxConnections);

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
        // This is because some cells have wrong heights and are considered noise (I think)
        var allRampCells = ramps.SelectMany(ramp => ramp.Cells).ToHashSet();
        var orderedNoise = noise.OrderBy(noisyCell => allRampCells.Min(rampCell => rampCell.DistanceTo(noisyCell)));
        foreach (var noisyCell in orderedNoise) {
            var noisyCellRampNeighbors = _terrainTracker.GetReachableNeighbors(noisyCell, allRampCells, considerObstaclesObstructions: false);

            var rampToAddTo = ramps.FirstOrDefault(ramp => noisyCellRampNeighbors.Any(ramp.Cells.Contains));
            if (rampToAddTo != default) {
                rampToAddTo.Cells.Add(noisyCell);
                allRampCells.Add(noisyCell);
                noise.Remove(noisyCell);
            }
        }

        return ramps;
    }

    /// <summary>
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
