using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.Wrapper;

namespace Bot;

public static class Clustering {
    private enum Labels {
        Noise,
        BorderPoint,
        CorePoint
    }

    public static (List<List<T>> clusters, List<T> noise) DBSCAN<T>(List<T> items, float epsilon, int minPoints) where T: class, IHavePosition {
        var clusters = new List<List<T>>();
        var labels = new Dictionary<T, Labels>();

        var currentCluster = new List<T>();
        foreach (var item in items) {
            if (labels.ContainsKey(item)) {
                continue;
            }

            var neighbors = items.Where(otherItem => item != otherItem && item.Position.DistanceTo(otherItem.Position) <= epsilon).ToList();
            if (neighbors.Count < minPoints) {
                labels[item] = Labels.Noise;
                continue;
            }

            labels[item] = Labels.CorePoint;
            currentCluster.Add(item);

            for (var i = 0; i < neighbors.Count; i++) {
                var neighbor = neighbors[i];

                if (labels.TryGetValue(neighbor, out var label)) {
                    if (label == Labels.Noise) {
                        labels[neighbor] = Labels.BorderPoint;
                        currentCluster.Add(neighbor);
                    }

                    continue;
                }

                currentCluster.Add(neighbor);

                var neighborsOfNeighbor = items.Where(otherItem => neighbor != otherItem && neighbor.Position.DistanceTo(otherItem.Position) <= epsilon).ToList();
                if (neighborsOfNeighbor.Count >= minPoints) {
                    labels[neighbor] = Labels.CorePoint;
                    neighbors.AddRange(neighborsOfNeighbor);
                }
                else {
                    labels[neighbor] = Labels.BorderPoint;
                }
            }

            clusters.Add(currentCluster);
            currentCluster = new List<T>();
        }

        clusters.ForEach(DrawBoundingBox);

        var noise = new List<T>();
        foreach(var (item, label) in labels) {
            if (label == Labels.Noise) {
                noise.Add(item);
            }
        }

        return (clusters, noise);
    }

    public static Vector3 GetCenter(List<Vector3> cluster) {
        if (cluster.Count <= 0) {
            Logger.Error("Trying to GetCenter of an empty cluster");

            return default;
        }

        var avgX = cluster.Average(position => position.X);
        var avgY = cluster.Average(position => position.Y);

        return new Vector3(avgX, avgY, 0).WithWorldHeight();
    }

    public static Vector3 GetCenter<T>(List<T> cluster) where T: class, IHavePosition {
        if (cluster.Count <= 0) {
            Logger.Error("Trying to GetCenter of an empty cluster");

            return default;
        }

        var avgX = cluster.Average(element => element.Position.X);
        var avgY = cluster.Average(element => element.Position.Y);

        return new Vector3(avgX, avgY, 0).WithWorldHeight();
    }

    public static Vector3 GetBoundingBoxCenter<T>(List<T> cluster) where T: class, IHavePosition {
        var minX = cluster.Select(element => element.Position.X).Min();
        var maxX = cluster.Select(element => element.Position.X).Max();
        var minY = cluster.Select(element => element.Position.Y).Min();
        var maxY = cluster.Select(element => element.Position.Y).Max();

        var centerX = minX + (maxX - minX) / 2;
        var centerY = minY + (maxY - minY) / 2;

        return new Vector3(centerX, centerY, 0).WithWorldHeight();
    }

    private static void DrawBoundingBox<T>(IReadOnlyCollection<T> cluster) where T: class, IHavePosition {
        var minX = cluster.Select(unit => unit.Position.X).Min();
        var maxX = cluster.Select(unit => unit.Position.X).Max();
        var minY = cluster.Select(unit => unit.Position.Y).Min();
        var maxY = cluster.Select(unit => unit.Position.Y).Max();

        var centerX = minX + (maxX - minX) / 2;
        var centerY = minY + (maxY - minY) / 2;
        var boundingBoxCenter = new Vector3(centerX, centerY, 0).WithWorldHeight();

        Program.GraphicalDebugger.AddRectangle(boundingBoxCenter, maxX - minX, maxY - minY, Colors.Orange);
    }
}
