using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.Wrapper;

namespace Bot.MapKnowledge;

public static class PathProximityChokeFinder {
    public class ChokePoint {
        public Vector3 Start { get; }
        public Vector3 End { get; }
        public HashSet<Vector3> Edge { get; }

        public ChokePoint(Vector3 start, Vector3 end) {
            Start = start;
            End = end;
            Edge = GetPointsInBetween(start, end);
        }
    }

    public static List<ChokePoint> FindChokePoints() {
        var pathCells = GetPathsBetweenExpands();
        var chokeBorders = FindChokeBorders(pathCells);
        var chokeNodes = FindChokeNodes(chokeBorders);
        var chokePoints = FindChokePoints(chokeNodes);

        Logger.Info("Found {0} choke points", chokePoints.Count);

        return chokePoints;
    }

    private static HashSet<Vector3> GetPathsBetweenExpands() {
        // Find paths between each expand. These paths will tend to traverse regions but hug choke points
        var pathCells = new HashSet<Vector3>();
        foreach (var origin in ExpandAnalyzer.ExpandLocations) {
            foreach (var destination in ExpandAnalyzer.ExpandLocations) {
                var path = Pathfinder.FindPath(origin, destination, includeObstacles: false);
                foreach (var cell in path) {
                    pathCells.Add(cell);
                }
            }
        }

        foreach (var pathCell in pathCells) {
            // GraphicalDebugger.AddGridSquare(pathCell, Colors.LightGreen);
        }

        return pathCells;
    }

    private static HashSet<Vector3> FindChokeBorders(HashSet<Vector3> pathCells) {
        // Find cells bordering chokes
        var chokeCells = pathCells.SelectMany(cell => cell
                .GetNeighbors(distance: 2)
                .Where(neighbor => !MapAnalyzer.IsWalkable(neighbor, includeObstacles: false)) // Unwalkable cells near paths should be part of choke points
        );

        var chokeBorders = chokeCells.Select(chokeEdge => chokeEdge.WithWorldHeight()).ToHashSet();

        // Omit cells near ramps, we already know about them
        var rampNeighbors = RegionAnalyzer.Ramps.SelectMany(ramp => ramp.SelectMany(rampCell => rampCell.GetNeighbors(distance: 3))).Select(rampNeighbor => rampNeighbor.WithWorldHeight());
        foreach (var rampNeighbor in rampNeighbors) {
            if (chokeBorders.Contains(rampNeighbor)) {
                chokeBorders.Remove(rampNeighbor);
            }
        }

        foreach (var chokeCell in chokeBorders) {
            GraphicalDebugger.AddGridSquare(chokeCell, Colors.Red);
        }

        return chokeBorders;
    }

    private static List<Vector3> FindChokeNodes(HashSet<Vector3> chokeBorders) {
        // Cluster choke cells to find choke nodes
        var chokeMapCells = chokeBorders.Select(chokeCell => new RegionAnalyzer.MapCell(chokeCell.X, chokeCell.Y, withWorldHeight: false)).ToList();
        var chokeClusters = Clustering.DBSCAN(chokeMapCells, epsilon: 2.02f, minPoints: 5).clusters;

        // Get center of clusters as choke nodes
        var chokeNodes = new List<Vector3>();
        foreach (var chokeCluster in chokeClusters) {
            var chokeNode = Clustering.GetCenter(chokeCluster);
            chokeNode.Z = chokeCluster.Max(chokeEdge => chokeEdge.Position.WithWorldHeight().Z);

            foreach (var chokeCell in chokeCluster) {
                GraphicalDebugger.AddGridSquare(new Vector3(chokeCell.Position.X, chokeCell.Position.Y, chokeNode.Z), Colors.MulberryRed);
            }

            chokeNodes.Add(chokeNode);
            GraphicalDebugger.AddSphere(chokeNode, radius: 3, Colors.LightRed);
        }

        return chokeNodes;
    }

    static List<ChokePoint> FindChokePoints(List<Vector3> chokeNodes) {
        // Find choke edges by linking choke nodes
        var chokePoints = new List<ChokePoint>();
        foreach (var chokeNode in chokeNodes) {
            var closestChokeNodes = chokeNodes
                .OrderBy(other => chokeNode.DistanceTo(other))
                .Skip(1)
                .Where(closestChokeNode => GetNonRampWalkableRatio(chokeNode, closestChokeNode) > 0.33) // Ignore nodes that link through unwalkable terrain
                .Where(closestChokeNode => chokeNode.HorizontalDistanceTo(closestChokeNode) < 20)
                .Take(2);

            foreach (var closestChokeNode in closestChokeNodes) {
                GraphicalDebugger.AddLine(chokeNode.Translate(zTranslation: 3), closestChokeNode.Translate(zTranslation: 3), Colors.LightRed);
                foreach (var point in GetPointsInBetween(chokeNode, closestChokeNode)) {
                    GraphicalDebugger.AddGridSquare(point, Colors.LightRed);
                }

                chokePoints.Add(new ChokePoint(chokeNode, closestChokeNode));
            }
        }

        return chokePoints;
    }

    private static float GetNonRampWalkableRatio(Vector3 origin, Vector3 destination) {
        var separation = GetPointsInBetween(origin, destination);
        var validSeparationPointCount = separation.Count(point => MapAnalyzer.IsWalkable(point, includeObstacles: false) && !RegionAnalyzer.Ramps.Any(ramp => ramp.Contains(point)));

        return (float)validSeparationPointCount / separation.Count;
    }

    private static HashSet<Vector3> GetPointsInBetween(Vector3 origin, Vector3 destination) {
        var maxDistance = origin.HorizontalDistanceTo(destination);
        var currentDistance = 0f;

        var pointsInBetween = new HashSet<Vector3>();
        while (currentDistance < maxDistance) {
            pointsInBetween.Add(origin.TranslateTowards(destination, currentDistance, ignoreZAxis: true).AsWorldGridCenter().WithWorldHeight());
            currentDistance += 0.5f;
        }

        return pointsInBetween;
    }
}
