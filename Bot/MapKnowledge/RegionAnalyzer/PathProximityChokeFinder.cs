using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.Wrapper;

namespace Bot.MapKnowledge;

// TODO GD Compare with the Balvet & Al algorithm
public static class PathProximityChokeFinder {
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
            // Program.GraphicalDebugger.AddGridSquare(pathCell, Colors.LightGreen);
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
        var rampNeighbors = RegionAnalyzer.Ramps.SelectMany(ramp => ramp.SelectMany(rampCell => rampCell.GetNeighbors(distance: 3))).Select(rampNeighbor => rampNeighbor.ToVector3().WithWorldHeight());
        foreach (var rampNeighbor in rampNeighbors) {
            if (chokeBorders.Contains(rampNeighbor)) {
                chokeBorders.Remove(rampNeighbor);
            }
        }

        foreach (var chokeCell in chokeBorders) {
            Program.GraphicalDebugger.AddGridSquare(chokeCell, Colors.Red);
        }

        return chokeBorders;
    }

    private static List<Vector3> FindChokeNodes(HashSet<Vector3> chokeBorders) {
        // Cluster choke cells to find choke nodes
        var chokeMapCells = chokeBorders.Select(chokeCell => new MapCell(chokeCell.X, chokeCell.Y, withWorldHeight: false)).ToList();
        var chokeClusters = Clustering.DBSCAN(chokeMapCells, epsilon: 2.02f, minPoints: 5).clusters;

        // Get center of clusters as choke nodes
        var chokeNodes = new List<Vector3>();
        foreach (var chokeCluster in chokeClusters) {
            var chokeNode = Clustering.GetCenter(chokeCluster);
            chokeNode.Z = chokeCluster.Max(chokeEdge => chokeEdge.Position.WithWorldHeight().Z);

            foreach (var chokeCell in chokeCluster) {
                Program.GraphicalDebugger.AddGridSquare(new Vector3(chokeCell.Position.X, chokeCell.Position.Y, chokeNode.Z), Colors.MulberryRed);
            }

            chokeNodes.Add(chokeNode);
            Program.GraphicalDebugger.AddSphere(chokeNode, radius: 3, Colors.LightRed);
        }

        return chokeNodes;
    }

    private static List<ChokePoint> FindChokePoints(List<Vector3> chokeNodes) {
        // Find choke edges by linking choke nodes
        var chokePoints = new List<ChokePoint>();
        foreach (var chokeNode in chokeNodes) {
            var closestChokeNodes = chokeNodes
                .OrderBy(other => chokeNode.DistanceTo(other))
                .Skip(1)
                .Where(closestChokeNode => GetNonRampWalkableRatio(chokeNode, closestChokeNode) > 0.33) // Ignore nodes that link through unwalkable terrain
                .Where(closestChokeNode => chokeNode.HorizontalDistanceTo(closestChokeNode) < 19);

            foreach (var closestChokeNode in closestChokeNodes) {
                Program.GraphicalDebugger.AddLine(chokeNode.Translate(zTranslation: 3), closestChokeNode.Translate(zTranslation: 3), Colors.LightRed);
                foreach (var point in chokeNode.GetPointsInBetween(closestChokeNode)) {
                    Program.GraphicalDebugger.AddGridSquare(point, Colors.LightRed);
                }

                chokePoints.Add(new ChokePoint(chokeNode, closestChokeNode));
            }
        }

        return chokePoints;
    }

    private static float GetNonRampWalkableRatio(Vector3 origin, Vector3 destination) {
        var separation = origin.GetPointsInBetween(destination);
        var validSeparationPointCount = separation.Count(point => MapAnalyzer.IsWalkable(point, includeObstacles: false) && !RegionAnalyzer.Ramps.Any(ramp => ramp.Contains(point.ToVector2())));

        return (float)validSeparationPointCount / separation.Count;
    }
}
