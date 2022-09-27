using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.Wrapper;

namespace Bot.MapKnowledge;

// Incomplete, unclear if it can work
public static class MapFrontierChokeFinder {
    private class Node : IHavePosition {
        public Vector3 Position { get; }
        public List<Node> Neighbors { get; set; }

        public Node(int x, int y) {
            Position = new Vector3(x, y, 0).AsWorldGridCenter();
        }
    }

    private class MapFrontierGraph {
        public HashSet<Node> AllNodes { get; }
        public List<List<Node>> Islands { get; }

        public MapFrontierGraph(List<List<Node>> islands) {
            AllNodes = new HashSet<Node>(islands.SelectMany(set => set));
            Islands = islands;

            foreach (var set in islands) {
                foreach (var node in set) {
                    node.Neighbors = set
                        .Where(potentialNeighbor => node.Position.HorizontalDistanceTo(potentialNeighbor.Position) <= 1.5f) // 1.5f is slightly more than the diagonal distance (1.41)
                        .ToList();
                }
            }
        }
    }

    public static List<ChokePoint> FindChokePoints() {
        var mapFrontierGraph = BuildMapFrontierGraph();
        ComputeBestShortcutOfEachNode(mapFrontierGraph);
        PruneShortcuts(mapFrontierGraph);
        var chokePoints = BuildChokePoints(mapFrontierGraph);

        Logger.Info("Found {0} frontier cells, {1} islands and {2} choke points", mapFrontierGraph.AllNodes.Count, mapFrontierGraph.Islands.Count, chokePoints.Count);

        DebugFrontier(mapFrontierGraph);

        return chokePoints;
    }

    private static void DebugFrontier(MapFrontierGraph mapFrontierGraph) {
        for (var i = 0; i < mapFrontierGraph.Islands.Count; i++) {
            foreach (var node in mapFrontierGraph.Islands[i]) {
                Program.GraphicalDebugger.AddGridSquare(node.Position.WithWorldHeight(), Colors.Orange);
                Program.GraphicalDebugger.AddText(i.ToString(), worldPos: node.Position.WithWorldHeight().ToPoint());
            }
        }
    }

    private static MapFrontierGraph BuildMapFrontierGraph() {
        var frontier = new List<Node>();

        for (var x = 0; x < MapAnalyzer.MaxX; x++) {
            for (var y = 0; y < MapAnalyzer.MaxY; y++) {
                var node = new Node(x, y);
                if (MapAnalyzer.IsWalkable(node.Position, includeObstacles: false)
                    && node.Position.GetNeighbors().Any(neighbor => !MapAnalyzer.IsWalkable(neighbor, includeObstacles: false))) {
                    frontier.Add(node);
                }
            }
        }

        var (islands, noise) = Clustering.DBSCAN(frontier, 1.5f, 1); // 1.5f is slightly more than the diagonal distance (1.41)
        if (noise.Count > 0) {
            Logger.Warning("Unexpectedly classified {0} map cells as noise during map frontier graph building", noise.Count);
        }

        return new MapFrontierGraph(islands);
    }

    private static void ComputeBestShortcutOfEachNode(MapFrontierGraph mapFrontierMapFrontierGraph) {
        // For each node in the graph
        // Find closest from other clusters

        // Find closest in our cluster when cluster distance > threshold * horizontal distance
        // Note: Should be reachable without crossing another frontier cell (except maybe immediate neighbors)
    }

    private static void PruneShortcuts(MapFrontierGraph mapFrontierMapFrontierGraph) {

    }

    private static List<ChokePoint> BuildChokePoints(MapFrontierGraph mapFrontierMapFrontierGraph) {
        return new List<ChokePoint>();
    }
}
