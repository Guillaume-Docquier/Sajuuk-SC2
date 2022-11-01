using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;

namespace Bot.MapKnowledge;

using Path = List<Vector2>;

public static class Pathfinder {
    private const bool DrawEnabled = true; // TODO GD Flag this

    public static readonly Dictionary<Vector2, Dictionary<Vector2, Path>> Memory = new Dictionary<Vector2, Dictionary<Vector2, List<Vector2>>>();

    /// <summary>
    /// <para>Finds a path between the origin and destination.</para>
    /// <para>The pathing considers rocks but not buildings or units.</para>
    /// <para>The results are cached so subsequent calls with the same origin and destinations are free.</para>
    /// </summary>
    /// <param name="origin">The origin position.</param>
    /// <param name="destination">The destination position.</param>
    /// <param name="includeObstacles">If you're wondering if you should be using this, you shouldn't.</param>
    /// <returns>The requested path, or null if the destination is unreachable from the origin.</returns>
    public static Path FindPath(Vector2 origin, Vector2 destination, bool includeObstacles = true) {
        // Improve caching performance
        origin = origin.ClosestWalkable().AsWorldGridCorner();
        destination = destination.ClosestWalkable().AsWorldGridCorner();

        if (origin == destination) {
            return new Path();
        }

        var isPathKnown = TryGetPathFromMemory(origin, destination, out var knownPath);
        if (isPathKnown) {
            DebugPath(knownPath, true);
            return knownPath;
        }

        var maybeNullPath = AStar(origin, destination, (from, to) => from.DistanceTo(to), includeObstacles);
        if (maybeNullPath == null) {
            Logger.Info("No path found between {0} and {1}", origin, destination);
            SavePathToMemory(origin, destination, null);
            return null;
        }

        var path = maybeNullPath.Select(step => step.AsWorldGridCenter()).ToList();
        DebugPath(path, false);

        SavePathToMemory(origin, destination, path);

        return path;
    }

    /// <summary>
    /// Draws a path in green if it is known, or in blue if it has just been calculated.
    /// Nothing is drawn if drawing is not enabled.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="isKnown"></param>
    private static void DebugPath(List<Vector2> path, bool isKnown) {
        if (!DrawEnabled) {
            return;
        }

        if (isKnown) {
            Program.GraphicalDebugger.AddPath(path, Colors.LightGreen, Colors.DarkGreen);
        }
        else {
            Program.GraphicalDebugger.AddPath(path, Colors.LightBlue, Colors.DarkBlue);
        }
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
    private static IEnumerable<Vector2> AStar(Vector2 origin, Vector2 destination, Func<Vector2, Vector2, float> getEdgeLength, bool includeObstacles = true) {
        var cameFrom = new Dictionary<Vector2, Vector2>();

        var gScore = new Dictionary<Vector2, float>
        {
            [origin] = 0,
        };

        var fScore = new Dictionary<Vector2, float>
        {
            [origin] = getEdgeLength(origin, destination),
        };

        var openSetContents = new HashSet<Vector2>
        {
            origin,
        };
        var openSet = new PriorityQueue<Vector2, float>();
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

    private static IEnumerable<Vector2> BuildPath(IReadOnlyDictionary<Vector2, Vector2> cameFrom, Vector2 end) {
        var current = end;
        var path = new List<Vector2> { current };
        while (cameFrom.ContainsKey(current)) {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();

        return path;
    }

    private static bool TryGetPathFromMemory(Vector2 origin, Vector2 destination, out Path path) {
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

    private static void SavePathToMemory(Vector2 origin, Vector2 destination, Path path) {
        if (!Memory.ContainsKey(origin)) {
            Memory[origin] = new Dictionary<Vector2, Path> { [destination] = path };
        }
        else {
            Memory[origin][destination] = path;
        }
    }
}
