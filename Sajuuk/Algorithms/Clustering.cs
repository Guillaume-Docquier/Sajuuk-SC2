using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameSense;

namespace Sajuuk.Algorithms;

public class Clustering : IClustering {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;

    private const bool DrawEnabled = false; // TODO GD Flag this

    // ReSharper disable once InconsistentNaming
    private enum DBSCANLabels {
        Noise,
        BorderPoint,
        CorePoint
    }

    public Clustering(
        ITerrainTracker terrainTracker,
        IGraphicalDebugger graphicalDebugger
    ) {
        _terrainTracker = terrainTracker;
        _graphicalDebugger = graphicalDebugger;
    }

    /// <summary>
    /// Performs a flood fill on the given cells, starting from the provided starting point.
    /// </summary>
    /// <param name="cells">The cells to perform flood fill on.</param>
    /// <param name="startingPoint">The starting point for the flood fill.</param>
    /// <returns>The cells reached by the flood fill.</returns>
    public IEnumerable<Vector2> FloodFill(IReadOnlySet<Vector2> cells, Vector2 startingPoint) {
        var toExplore = cells.ToHashSet();
        toExplore.Remove(startingPoint);

        var explored = new HashSet<Vector2>();

        var explorationQueue = new Queue<Vector2>();
        explorationQueue.Enqueue(startingPoint);

        while (explorationQueue.Any()) {
            var cellToExplore = explorationQueue.Dequeue();
            explored.Add(cellToExplore);

            var neighbors = _terrainTracker.GetReachableNeighbors(cellToExplore, toExplore, considerObstaclesObstructions: false).ToList();
            foreach (var neighbor in neighbors) {
                explorationQueue.Enqueue(neighbor);
                toExplore.Remove(neighbor);
            }
        }

        return explored;
    }

    public (List<List<Vector2>> clusters, List<Vector2> noise) DBSCAN(IReadOnlyCollection<Vector2> positions, float epsilon, int minPoints) {
        var clusters = new List<List<Vector2>>();
        var labels = new Dictionary<Vector2, DBSCANLabels>();

        var currentCluster = new List<Vector2>();
        foreach (var position in positions) {
            if (labels.ContainsKey(position)) {
                continue;
            }

            var neighbors = positions.Where(otherItem => position != otherItem && position.DistanceTo(otherItem) <= epsilon).ToList();
            if (neighbors.Count < minPoints) {
                labels[position] = DBSCANLabels.Noise;
                continue;
            }

            labels[position] = DBSCANLabels.CorePoint;
            currentCluster.Add(position);

            for (var i = 0; i < neighbors.Count; i++) {
                var neighbor = neighbors[i];

                if (labels.TryGetValue(neighbor, out var label)) {
                    if (label == DBSCANLabels.Noise) {
                        labels[neighbor] = DBSCANLabels.BorderPoint;
                        currentCluster.Add(neighbor);
                    }

                    continue;
                }

                currentCluster.Add(neighbor);

                var neighborsOfNeighbor = positions.Where(otherPosition => neighbor != otherPosition && neighbor.DistanceTo(otherPosition) <= epsilon).ToList();
                if (neighborsOfNeighbor.Count >= minPoints) {
                    labels[neighbor] = DBSCANLabels.CorePoint;
                    neighbors.AddRange(neighborsOfNeighbor);
                }
                else {
                    labels[neighbor] = DBSCANLabels.BorderPoint;
                }
            }

            clusters.Add(currentCluster);
            currentCluster = new List<Vector2>();
        }

        clusters.ForEach(DrawBoundingBox);

        var noise = new List<Vector2>();
        foreach(var (position, label) in labels) {
            if (label == DBSCANLabels.Noise) {
                noise.Add(position);
            }
        }

        return (clusters, noise);
    }

    public (List<List<Vector3>> clusters, List<Vector3> noise) DBSCAN(IReadOnlyCollection<Vector3> positions, float epsilon, int minPoints) {
        var clusters = new List<List<Vector3>>();
        var labels = new Dictionary<Vector3, DBSCANLabels>();

        var currentCluster = new List<Vector3>();
        foreach (var position in positions) {
            if (labels.ContainsKey(position)) {
                continue;
            }

            var neighbors = positions.Where(otherPosition => position != otherPosition && position.DistanceTo(otherPosition) <= epsilon).ToList();
            if (neighbors.Count < minPoints) {
                labels[position] = DBSCANLabels.Noise;
                continue;
            }

            labels[position] = DBSCANLabels.CorePoint;
            currentCluster.Add(position);

            for (var i = 0; i < neighbors.Count; i++) {
                var neighbor = neighbors[i];

                if (labels.TryGetValue(neighbor, out var label)) {
                    if (label == DBSCANLabels.Noise) {
                        labels[neighbor] = DBSCANLabels.BorderPoint;
                        currentCluster.Add(neighbor);
                    }

                    continue;
                }

                currentCluster.Add(neighbor);

                var neighborsOfNeighbor = positions.Where(otherPosition => neighbor != otherPosition && neighbor.DistanceTo(otherPosition) <= epsilon).ToList();
                if (neighborsOfNeighbor.Count >= minPoints) {
                    labels[neighbor] = DBSCANLabels.CorePoint;
                    neighbors.AddRange(neighborsOfNeighbor);
                }
                else {
                    labels[neighbor] = DBSCANLabels.BorderPoint;
                }
            }

            clusters.Add(currentCluster);
            currentCluster = new List<Vector3>();
        }

        clusters.ForEach(DrawBoundingBox);

        var noise = labels
            .Where(kv => kv.Value == DBSCANLabels.Noise)
            .Select(kv => kv.Key)
            .ToList();

        return (clusters, noise);
    }

    public (List<List<T>> clusters, List<T> noise) DBSCAN<T>(IReadOnlyCollection<T> items, float epsilon, int minPoints) where T: class, IHavePosition {
        var clusters = new List<List<T>>();
        var labels = new Dictionary<T, DBSCANLabels>();

        var currentCluster = new List<T>();
        foreach (var item in items) {
            if (labels.ContainsKey(item)) {
                continue;
            }

            var neighbors = items.Where(otherItem => item != otherItem && item.Position.DistanceTo(otherItem.Position) <= epsilon).ToList();
            if (neighbors.Count < minPoints) {
                labels[item] = DBSCANLabels.Noise;
                continue;
            }

            labels[item] = DBSCANLabels.CorePoint;
            currentCluster.Add(item);

            for (var i = 0; i < neighbors.Count; i++) {
                var neighbor = neighbors[i];

                if (labels.TryGetValue(neighbor, out var label)) {
                    if (label == DBSCANLabels.Noise) {
                        labels[neighbor] = DBSCANLabels.BorderPoint;
                        currentCluster.Add(neighbor);
                    }

                    continue;
                }

                currentCluster.Add(neighbor);

                var neighborsOfNeighbor = items.Where(otherItem => neighbor != otherItem && neighbor.Position.DistanceTo(otherItem.Position) <= epsilon).ToList();
                if (neighborsOfNeighbor.Count >= minPoints) {
                    labels[neighbor] = DBSCANLabels.CorePoint;
                    neighbors.AddRange(neighborsOfNeighbor);
                }
                else {
                    labels[neighbor] = DBSCANLabels.BorderPoint;
                }
            }

            clusters.Add(currentCluster);
            currentCluster = new List<T>();
        }

        clusters.ForEach(DrawBoundingBox);

        var noise = new List<T>();
        foreach(var (item, label) in labels) {
            if (label == DBSCANLabels.Noise) {
                noise.Add(item);
            }
        }

        return (clusters, noise);
    }

    public Vector2 GetCenter<T>(IEnumerable<T> cluster) where T: class, IHavePosition {
        return GetCenter(cluster.Select(item => item.Position.ToVector2()).ToList());
    }

    public Vector2 GetCenter(List<Vector2> cluster) {
        if (cluster.Count <= 0) {
            Logger.Error("Trying to GetCenter of an empty cluster");

            return default;
        }

        var avgX = cluster.Average(position => position.X);
        var avgY = cluster.Average(position => position.Y);

        return new Vector2(avgX, avgY);
    }

    public Vector3 GetBoundingBoxCenter<T>(IEnumerable<T> cluster) where T: class, IHavePosition {
        return GetBoundingBoxCenter(cluster.Select(item => item.Position.ToVector2()).ToList());
    }

    public Vector3 GetBoundingBoxCenter(List<Vector2> cluster) {
        var minX = cluster.Select(element => element.X).Min();
        var maxX = cluster.Select(element => element.X).Max();
        var minY = cluster.Select(element => element.Y).Min();
        var maxY = cluster.Select(element => element.Y).Max();

        var centerX = minX + (maxX - minX) / 2;
        var centerY = minY + (maxY - minY) / 2;

        return _terrainTracker.WithWorldHeight(new Vector2(centerX, centerY));
    }

    private void DrawBoundingBox<T>(IReadOnlyCollection<T> cluster) where T: class, IHavePosition {
        DrawBoundingBox(cluster.Select(item => item.Position.ToVector2()).ToList());
    }

    private void DrawBoundingBox(IReadOnlyCollection<Vector3> cluster) {
        DrawBoundingBox(cluster.Select(position => position.ToVector2()).ToList());
    }

    private void DrawBoundingBox(IReadOnlyCollection<Vector2> cluster) {
        if (!DrawEnabled) {
            return;
        }

        var minX = cluster.Select(unit => unit.X).Min();
        var maxX = cluster.Select(unit => unit.X).Max();
        var minY = cluster.Select(unit => unit.Y).Min();
        var maxY = cluster.Select(unit => unit.Y).Max();

        var centerX = minX + (maxX - minX) / 2;
        var centerY = minY + (maxY - minY) / 2;
        var boundingBoxCenter = _terrainTracker.WithWorldHeight(new Vector2(centerX, centerY));

        _graphicalDebugger.AddRectangle(boundingBoxCenter, maxX - minX, maxY - minY, Colors.Orange);
    }
}
