using System.Numerics;
using Algorithms.ExtensionMethods;

namespace Algorithms;

/// <summary>
/// A collection of clustering algorithms.
/// </summary>
public static class Clustering {
    private enum DBSCANLabels {
        Noise,
        BorderPoint,
        CorePoint
    }

    /// <summary>
    /// Performs a flood fill on the given cells, starting from the provided starting point.
    /// </summary>
    /// <param name="cells">The cells to perform flood fill on.</param>
    /// <param name="startingPoint">The starting point for the flood fill.</param>
    /// <param name="getReachableNeighbors">A function that receives a cell and a set of cells to explore and returns the list of reachable neighbors.</param>
    /// <returns>The cells reached by the flood fill.</returns>
    public static IEnumerable<Vector2> FloodFill(IEnumerable<Vector2> cells, Vector2 startingPoint, Func<Vector2, HashSet<Vector2>, List<Vector2>> getReachableNeighbors) {
        var toExplore = cells.ToHashSet();
        toExplore.Remove(startingPoint);

        var explored = new HashSet<Vector2>();

        var explorationQueue = new Queue<Vector2>();
        explorationQueue.Enqueue(startingPoint);

        while (explorationQueue.Any()) {
            var cellToExplore = explorationQueue.Dequeue();
            explored.Add(cellToExplore);

            var neighbors = getReachableNeighbors(cellToExplore, toExplore).ToList();
            foreach (var neighbor in neighbors) {
                explorationQueue.Enqueue(neighbor);
                toExplore.Remove(neighbor);
            }
        }

        return explored;
    }

    public static (List<List<Vector2>> clusters, List<Vector2> noise) DBSCAN(IReadOnlyCollection<Vector2> positions, float epsilon, int minPoints) {
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

        var noise = new List<Vector2>();
        foreach(var (position, label) in labels) {
            if (label == DBSCANLabels.Noise) {
                noise.Add(position);
            }
        }

        return (clusters, noise);
    }

    public static (List<List<Vector3>> clusters, List<Vector3> noise) DBSCAN(IReadOnlyCollection<Vector3> positions, float epsilon, int minPoints) {
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

        var noise = labels
            .Where(kv => kv.Value == DBSCANLabels.Noise)
            .Select(kv => kv.Key)
            .ToList();

        return (clusters, noise);
    }

    public static (List<List<T>> clusters, List<T> noise) DBSCAN<T>(IReadOnlyCollection<T> items, float epsilon, int minPoints) where T: class, IHavePosition {
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

        var noise = new List<T>();
        foreach(var (item, label) in labels) {
            if (label == DBSCANLabels.Noise) {
                noise.Add(item);
            }
        }

        return (clusters, noise);
    }

    public static Vector2 GetCenter(IEnumerable<IHavePosition> items) {
        return GetCenter(items.Select(item => item.Position.ToVector2()).ToList());
    }

    public static Vector2 GetCenter(List<Vector2> cells) {
        if (cells.Count <= 0) {
            throw new ArgumentException("Trying to GetCenter of an empty set of cells");
        }

        var avgX = cells.Average(position => position.X);
        var avgY = cells.Average(position => position.Y);

        return new Vector2(avgX, avgY);
    }

    public static Vector2 GetBoundingBoxCenter(List<Vector2> cluster) {
        var minX = cluster.Select(element => element.X).Min();
        var maxX = cluster.Select(element => element.X).Max();
        var minY = cluster.Select(element => element.Y).Min();
        var maxY = cluster.Select(element => element.Y).Max();

        var centerX = minX + (maxX - minX) / 2;
        var centerY = minY + (maxY - minY) / 2;

        return new Vector2(centerX, centerY);
    }
}
