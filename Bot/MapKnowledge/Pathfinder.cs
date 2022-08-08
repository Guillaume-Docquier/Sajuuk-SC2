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

        var isPathKnown = TryGetPathFromMemory(origin, destination, out var knownPath);
        if (isPathKnown) {
            return knownPath;
        }

        var maybeNullPath = AStar(origin, destination, (from, to) => from.HorizontalDistanceTo(to));
        if (maybeNullPath == null) {
            Logger.Info("No path found between {0} and {1}", origin, destination);
            SavePathToMemory(origin, destination, null);
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

            foreach (var neighbor in GetReachableNeighbors(current)) {
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

    // Diagonal makes more direct paths, but you can't walk diagonally if both sides of the diagonal are blocked
    private static IEnumerable<Vector3> GetReachableNeighbors(Vector3 position) {
        var leftPos = position.Translate(xTranslation: -1);
        var isLeftOk = MapAnalyzer.IsInBounds(leftPos) && MapAnalyzer.IsWalkable(leftPos);
        if (isLeftOk) {
            yield return leftPos;
        }

        var rightPos = position.Translate(xTranslation: 1);
        var isRightOk = MapAnalyzer.IsInBounds(rightPos) && MapAnalyzer.IsWalkable(rightPos);
        if (isRightOk) {
            yield return rightPos;
        }

        var upPos = position.Translate(yTranslation: 1);
        var isUpOk = MapAnalyzer.IsInBounds(upPos) && MapAnalyzer.IsWalkable(upPos);
        if (isUpOk) {
            yield return upPos;
        }

        var downPos = position.Translate(yTranslation: -1);
        var isDownOk = MapAnalyzer.IsInBounds(downPos) && MapAnalyzer.IsWalkable(downPos);
        if (isDownOk) {
            yield return downPos;
        }

        if (isLeftOk || isUpOk) {
            var leftUpPos = position.Translate(xTranslation: -1, yTranslation: 1);
            if (MapAnalyzer.IsInBounds(leftUpPos) && MapAnalyzer.IsWalkable(leftUpPos)) {
                yield return leftUpPos;
            }
        }

        if (isLeftOk || isDownOk) {
            var leftDownPos = position.Translate(xTranslation: -1, yTranslation: -1);
            if (MapAnalyzer.IsInBounds(leftDownPos) && MapAnalyzer.IsWalkable(leftDownPos)) {
                yield return leftDownPos;
            }
        }

        if (isRightOk || isUpOk) {
            var rightUpPos = position.Translate(xTranslation: 1, yTranslation: 1);
            if (MapAnalyzer.IsInBounds(rightUpPos) && MapAnalyzer.IsWalkable(rightUpPos)) {
                yield return rightUpPos;
            }
        }

        if (isRightOk || isDownOk) {
            var rightDownPos = position.Translate(xTranslation: 1, yTranslation: -1);
            if (MapAnalyzer.IsInBounds(rightDownPos) && MapAnalyzer.IsWalkable(rightDownPos)) {
                yield return rightDownPos;
            }
        }
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

    private static bool TryGetPathFromMemory(Vector3 origin, Vector3 destination, out Path path) {
        if (Memory.ContainsKey(origin) && Memory[origin].ContainsKey(destination)) {
            path = Memory[origin][destination];
            return true;
        }

        if (Memory.ContainsKey(destination) && Memory[destination].ContainsKey(origin)) {
            path = Enumerable.Reverse(Memory[destination][origin]).ToList();
            return true;
        }

        path = null;
        return false;
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
