using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Wrapper;

namespace Bot;

public static class Clustering {
    private enum Labels {
        Noise,
        BorderPoint,
        CorePoint
    }

    public static List<List<Unit>> DBSCAN(List<Unit> units, float epsilon, int minPoints) {
        var clusters = new List<List<Unit>>();
        var labels = new Dictionary<Unit, Labels>();

        var currentCluster = new List<Unit>();
        foreach (var unit in units) {
            if (labels.ContainsKey(unit)) {
                continue;
            }

            var neighbors = units.Where(otherUnit => unit != otherUnit && unit.DistanceTo(otherUnit) <= epsilon).ToList();
            if (neighbors.Count < minPoints) {
                labels[unit] = Labels.Noise;
                continue;
            }

            labels[unit] = Labels.CorePoint;
            currentCluster.Add(unit);

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

                var neighborsOfNeighbor = units.Where(otherUnit => neighbor != otherUnit && neighbor.DistanceTo(otherUnit) <= epsilon).ToList();
                if (neighborsOfNeighbor.Count >= minPoints) {
                    labels[neighbor] = Labels.CorePoint;
                    neighbors.AddRange(neighborsOfNeighbor);
                }
                else {
                    labels[neighbor] = Labels.BorderPoint;
                }
            }

            clusters.Add(currentCluster);
            currentCluster = new List<Unit>();
        }

        clusters.ForEach(DrawBoundingBox);

        return clusters;
    }

    public static Vector3 GetCenter(List<Unit> cluster) {
        var avgX = cluster.Average(soldier => soldier.Position.X);
        var avgY = cluster.Average(soldier => soldier.Position.Y);

        return new Vector3(avgX, avgY, 0).WithWorldHeight();
    }

    public static Vector3 GetBoundingBoxCenter(List<Unit> cluster) {
        var minX = cluster.Select(unit => unit.Position.X).Min();
        var maxX = cluster.Select(unit => unit.Position.X).Max();
        var minY = cluster.Select(unit => unit.Position.Y).Min();
        var maxY = cluster.Select(unit => unit.Position.Y).Max();

        var centerX = minX + (maxX - minX) / 2;
        var centerY = minY + (maxY - minY) / 2;

        return new Vector3(centerX, centerY, 0).WithWorldHeight();
    }

    private static void DrawBoundingBox(IReadOnlyCollection<Unit> cluster) {
        var minX = cluster.Select(unit => unit.Position.X).Min();
        var maxX = cluster.Select(unit => unit.Position.X).Max();
        var minY = cluster.Select(unit => unit.Position.Y).Min();
        var maxY = cluster.Select(unit => unit.Position.Y).Max();

        var centerX = minX + (maxX - minX) / 2;
        var centerY = minY + (maxY - minY) / 2;
        var boundingBoxCenter = new Vector3(centerX, centerY, 0).WithWorldHeight();

        GraphicalDebugger.AddRectangle(boundingBoxCenter, maxX - minX, maxY - minY, Colors.Orange);
    }
}
