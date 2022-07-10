using System.Collections.Generic;
using System.Linq;

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

        return clusters;
    }
}
