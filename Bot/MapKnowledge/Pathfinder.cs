using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.Wrapper;

namespace Bot.MapKnowledge;

using Path = List<Vector3>;

public static class Pathfinder {
    public static readonly Dictionary<Vector3, Dictionary<Vector3, Path>> Memory = new Dictionary<Vector3, Dictionary<Vector3, List<Vector3>>>();

    public static Path FindPath(Vector3 origin, Vector3 destination) {
        // Improve caching performance
        origin = origin.ClosestWalkable().AsWorldGridCorner().WithoutZ();
        destination = destination.ClosestWalkable().AsWorldGridCorner().WithoutZ();

        var knownPath = GetPathFromMemory(origin, destination);
        if (knownPath != null) {
            return knownPath;
        }

        var maybeNullPath = AStar(origin, destination, (from, to) => from.HorizontalDistanceTo(to));
        if (maybeNullPath == null) {
            Logger.Error("Path from {0} to {1} was null", origin, destination);
            return null;
        }

        var path = maybeNullPath.Select(step => step.AsWorldGridCenter()).ToList();

        GraphicalDebugger.AddSphere(origin.WithWorldHeight(), 1.5f, Colors.Cyan);
        GraphicalDebugger.AddSphere(destination.WithWorldHeight(), 1.5f, Colors.DarkBlue);
        for (var i = 0; i < path.Count; i++) {
            GraphicalDebugger.AddGridSquare(path[i], Colors.Gradient(Colors.Cyan, Colors.DarkBlue, (float)i / path.Count));
        }

        SavePathToMemory(origin, destination, path);

        return path;
    }

    private static IEnumerable<Vector3> AStar(Vector3 origin, Vector3 destination, Func<Vector3, Vector3, float> getHeuristicCost) {
        var cameFrom = new Dictionary<Vector3, Vector3>();

        var gScore = new Dictionary<Vector3, float>
        {
            [origin] = 0,
        };

        var fScore = new Dictionary<Vector3, float>
        {
            [origin] = getHeuristicCost(origin, destination),
        };

        var openSetContents = new HashSet<Vector3>
        {
            origin,
        };
        var openSet = new PriorityQueue<Vector3, float>();
        openSet.Enqueue(origin, fScore[origin]);

        while (openSet.Count > 0) {
            var current = openSet.Dequeue();
            openSetContents.Remove(current);

            if (current == destination) {
                return BuildPath(cameFrom, current);
            }

            foreach (var neighbor in GetNeighbors(current).Where(MapAnalyzer.IsInBounds).Where(MapAnalyzer.IsWalkable)) {
                var neighborGScore = gScore[current] + getHeuristicCost(current, neighbor);

                if (!gScore.ContainsKey(neighbor) || neighborGScore < gScore[neighbor]) {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = neighborGScore;
                    fScore[neighbor] = neighborGScore + getHeuristicCost(current, destination);

                    if (!openSetContents.Contains(neighbor)) {
                        openSetContents.Add(neighbor);
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                    }
                }
            }
        }

        return null;
    }

    private static IEnumerable<Vector3> GetNeighbors(Vector3 position) {
        yield return position.Translate(xTranslation: -1, yTranslation: -1);
        yield return position.Translate(xTranslation: -1);
        yield return position.Translate(xTranslation: -1, yTranslation: 1);

        yield return position.Translate(yTranslation: -1);
        yield return position.Translate(yTranslation: 1);

        yield return position.Translate(xTranslation: 1, yTranslation: -1);
        yield return position.Translate(xTranslation: 1);
        yield return position.Translate(xTranslation: 1, yTranslation: 1);
    }

    private static IEnumerable<Vector3> BuildPath(IReadOnlyDictionary<Vector3, Vector3> cameFrom, Vector3 end) {
        var current = end;
        var path = new List<Vector3> { current };
        while (cameFrom.ContainsKey(current)) {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();

        return path.Select(step => step.WithWorldHeight());
    }

    private static Path GetPathFromMemory(Vector3 origin, Vector3 destination) {
        if (Memory.ContainsKey(origin) && Memory[origin].ContainsKey(destination)) {
            return Memory[origin][destination];
        }

        if (Memory.ContainsKey(destination) && Memory[destination].ContainsKey(origin)) {
            var path = Memory[destination][origin];

            return Enumerable.Reverse(path).ToList();
        }

        return null;
    }

    private static void SavePathToMemory(Vector3 origin, Vector3 destination, Path path) {
        if (!Memory.ContainsKey(origin)) {
            Memory[origin] = new Dictionary<Vector3, Path> { [destination] = path };
        }
        else {
            Memory[origin][destination] = path;
        }
    }
}
