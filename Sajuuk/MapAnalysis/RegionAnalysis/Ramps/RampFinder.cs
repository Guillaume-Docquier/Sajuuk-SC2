using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.Algorithms;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameSense;

namespace Sajuuk.MapAnalysis.RegionAnalysis.Ramps;

public class RampFinder : IRampFinder {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IClustering _clustering;

    private readonly float _diagonalDistance = (float)Math.Sqrt(2);

    public RampFinder(ITerrainTracker terrainTracker, IClustering clustering) {
        _terrainTracker = terrainTracker;
        _clustering = clustering;
    }

    /// <summary>
    /// Identify ramps given cells that are walkable but not buildable.
    /// Some noise will be produced because some unbuildable cells are vision blockers and they should be used to find regions.
    /// </summary>
    /// <returns>
    /// The ramps and the cells that are not part of any ramp.
    /// </returns>
    public (List<HashSet<Vector2>> ramps, IEnumerable<MapCell> rampsNoise) FindRamps(IEnumerable<MapCell> walkableCells) {
        var potentialRampCells = walkableCells.Where(cell => !_terrainTracker.IsBuildable(cell.Position, considerObstaclesObstructions: false)).ToList();
        foreach (var potentialRampCell in potentialRampCells) {
            // We ignore the Z component to simplify clustering
            potentialRampCell.Position = potentialRampCell.Position with { Z = 0 };
        }

        var ramps = new List<HashSet<Vector2>>();
        var noise = new HashSet<MapCell>();

        // We cluster once for an initial split
        var weakClusteringResult = _clustering.DBSCAN(potentialRampCells, epsilon: 1, minPoints: 1);
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
        // This is because some cells have wrong heights and are considered noise (I think)
        var allRampCells = ramps.SelectMany(cells => cells).ToHashSet();
        var orderedNoise = noise.OrderBy(noisyCell => allRampCells.Min(rampCell => rampCell.DistanceTo(noisyCell.Position.ToVector2())));
        foreach (var noisyCell in orderedNoise) {
            var noisyCellAsVector2 = noisyCell.Position.ToVector2();
            var noisyCellRampNeighbors = _terrainTracker.GetReachableNeighbors(noisyCellAsVector2, allRampCells, considerObstaclesObstructions: false);

            var rampToAddTo = ramps.FirstOrDefault(ramp => noisyCellRampNeighbors.Any(ramp.Contains));
            if (rampToAddTo != default) {
                rampToAddTo.Add(noisyCellAsVector2);
                allRampCells.Add(noisyCellAsVector2);
                noise.Remove(noisyCell);
            }
        }

        foreach (var potentialRampCell in potentialRampCells) {
            potentialRampCell.Position = _terrainTracker.WithWorldHeight(potentialRampCell.Position); // Restore Z
        }

        return (ramps, noise);
    }

    /// <summary>
    /// A real ramp connects two different height layers with a progressive slope.
    /// If all the tiles in the given ramp are roughly on the same height, this is not a ramp.
    /// - It is probably a group of vision blockers, with are also walkable and unbuildable.
    /// </summary>
    /// <param name="rampCluster">The cells in a ramp</param>
    /// <returns>True if the tiles have varied heights that correspond to typical ramp characteristics, false otherwise</returns>
    private bool IsReallyARamp(IReadOnlyCollection<MapCell> rampCluster) {
        var nbDifferentHeights = rampCluster
            .Select(cell => _terrainTracker.WithWorldHeight(cell.Position).Z)
            .ToHashSet()
            .Count;

        // Ramps typically have nbDifferentHeights = [4, 9]
        if (nbDifferentHeights < 4) {
            return false;
        }

        var minHeight = rampCluster.Min(cell => _terrainTracker.WithWorldHeight(cell.Position).Z);
        var maxHeight = rampCluster.Max(cell => _terrainTracker.WithWorldHeight(cell.Position).Z);
        var heightDifference = Math.Abs(minHeight - maxHeight);

        // Ramps typically have heightDifference = [1.5, 2]
        return heightDifference > 1f;
    }
}
