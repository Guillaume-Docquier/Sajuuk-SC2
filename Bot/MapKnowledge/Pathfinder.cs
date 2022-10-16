using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;

namespace Bot.MapKnowledge;

using Path = List<Vector3>;

public static class Pathfinder {
    public static readonly Dictionary<Vector3, Dictionary<Vector3, Path>> Memory = new Dictionary<Vector3, Dictionary<Vector3, List<Vector3>>>();

    /// <summary>
    /// <para>Finds a path between the origin and destination.</para>
    /// <para>The pathing considers rocks but not buildings or units.</para>
    /// <para>The results are cached so subsequent calls with the same origin and destinations are free.</para>
    /// </summary>
    /// <param name="origin">The origin position.</param>
    /// <param name="destination">The destination position.</param>
    /// <param name="includeObstacles">If you're wondering if you should be using this, you shouldn't.</param>
    /// <returns>The requested path, or null if the destination is unreachable from the origin.</returns>
    public static Path FindPath(Vector3 origin, Vector3 destination, bool includeObstacles = true) {
        // Improve caching performance
        origin = origin.ClosestWalkable().AsWorldGridCorner().WithoutZ();
        destination = destination.ClosestWalkable().AsWorldGridCorner().WithoutZ();

        if (origin == destination) {
            return new Path();
        }

        var isPathKnown = TryGetPathFromMemory(origin, destination, out var knownPath);
        if (isPathKnown) {
            Program.GraphicalDebugger.AddPath(knownPath, Colors.LightGreen, Colors.DarkGreen);
            return knownPath;
        }

        var maybeNullPath = AStar(origin, destination, (from, to) => from.HorizontalDistanceTo(to), includeObstacles);
        if (maybeNullPath == null) {
            Logger.Info("No path found between {0} and {1}", origin, destination);
            SavePathToMemory(origin, destination, null);
            return null;
        }

        var path = maybeNullPath.Select(step => step.AsWorldGridCenter()).ToList();
        Program.GraphicalDebugger.AddPath(path, Colors.LightBlue, Colors.DarkBlue);

        SavePathToMemory(origin, destination, path);

        return path;
    }

    /// <summary>
    /// <para>A textbook implementation of the A* search algorithm.</para>
    /// <para>See https://en.wikipedia.org/wiki/A*_search_algorithm</para>
    /// </summary>
    /// <param name="origin">The origin position.</param>
    /// <param name="destination">The destination position.</param>
    /// <param name="getEdgeLength">A function that computes the distance between two nodes</param>
    /// <param name="includeObstacles">If you're wondering if you should be using this, you shouldn't.</param>
    /// <returns>The requested path, or null if the destination is unreachable from the origin.</returns>
    private static IEnumerable<Vector3> AStar(Vector3 origin, Vector3 destination, Func<Vector3, Vector3, float> getEdgeLength, bool includeObstacles = true) {
        var cameFrom = new Dictionary<Vector3, Vector3>();

        var gScore = new Dictionary<Vector3, float>
        {
            [origin] = 0,
        };

        var fScore = new Dictionary<Vector3, float>
        {
            [origin] = getEdgeLength(origin, destination),
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

            foreach (var neighbor in current.GetReachableNeighbors(includeObstacles)) {
                var neighborGScore = gScore[current] + getEdgeLength(current, neighbor);

                if (!gScore.ContainsKey(neighbor) || neighborGScore < gScore[neighbor]) {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = neighborGScore;
                    fScore[neighbor] = neighborGScore + getEdgeLength(current, destination);

                    if (!openSetContents.Contains(neighbor)) {
                        openSetContents.Add(neighbor);
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                    }
                }
            }
        }

        return null;
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
