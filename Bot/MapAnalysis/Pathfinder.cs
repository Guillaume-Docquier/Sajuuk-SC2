using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.MapAnalysis.RegionAnalysis;

namespace Bot.MapAnalysis;

using CellPath = List<Vector2>;
using IRegionPath = List<IRegion>;

public class Pathfinder {
    public static readonly Pathfinder Instance = new Pathfinder(TerrainTracker.Instance);

    private readonly ITerrainTracker _terrainTracker;

    private const bool DrawEnabled = false; // TODO GD Flag this

    /// <summary>
    /// This is public for the performance debugging report, please don't rely on this
    /// </summary>
    public readonly Dictionary<Vector2, Dictionary<Vector2, CellPath>> CellPathsMemory = new ();

    /// <summary>
    /// A multi-level cache for region pathfinding
    /// Used to store paths based on different sets of blocked regions (common use case)
    /// </summary>
    private readonly Dictionary<string, Dictionary<IRegion, Dictionary<IRegion, IRegionPath>>> _regionPathsMemory = new ();

    public Pathfinder(ITerrainTracker terrainTracker) {
        _terrainTracker = terrainTracker;
    }

    /// <summary>
    /// <para>Finds a path between the origin and destination.</para>
    /// <para>The pathing considers rocks but not buildings or units.</para>
    /// <para>The results are cached so subsequent calls with the same origin and destinations are free.</para>
    /// </summary>
    /// <param name="origin">The origin position.</param>
    /// <param name="destination">The destination position.</param>
    /// <returns>The requested path, or null if the destination is unreachable from the origin.</returns>
    public CellPath FindPath(Vector2 origin, Vector2 destination) {
        // Improve caching performance
        origin = _terrainTracker.GetClosestWalkable(origin).AsWorldGridCorner();
        destination = _terrainTracker.GetClosestWalkable(destination).AsWorldGridCorner();

        if (origin == destination) {
            return new CellPath();
        }

        if (TryGetPathFromMemory(origin, destination, CellPathsMemory, out var knownPath)) {
            DebugPath(knownPath, isKnown: true);
            return knownPath;
        }

        var maybeNullPath = AStar(origin, destination, (from, to) => from.DistanceTo(to), current => _terrainTracker.GetReachableNeighbors(current));
        if (maybeNullPath == null) {
            Logger.Info("No path found between {0} and {1}", origin, destination);
            SavePathToMemory(origin, destination, CellPathsMemory, null);
            return null;
        }

        var path = maybeNullPath.Select(step => step.AsWorldGridCenter()).ToList();
        DebugPath(path, isKnown: false);

        SavePathToMemory(origin, destination, CellPathsMemory, path);

        return path;
    }

    /// <summary>
    /// <para>Finds a path between the origin region and the destination region.</para>
    /// <para>The pathing considers rocks but not buildings or units.</para>
    /// <para>The results are cached so subsequent calls with the same origin and destinations are free.</para>
    /// </summary>
    /// <param name="origin">The origin region.</param>
    /// <param name="destination">The destination region.</param>
    /// <param name="excludedRegions">Regions that should be omitted from pathfinding</param>
    /// <returns>The requested path, or null if the destination is unreachable from the origin.</returns>
    public IRegionPath FindPath(IRegion origin, IRegion destination, HashSet<IRegion> excludedRegions = null) {
        if (origin == destination) {
            return new IRegionPath();
        }

        excludedRegions ??= new HashSet<IRegion>();
        var regionMemoryKey = GetRegionMemoryKey(excludedRegions);
        var regionMemory = GetRegionMemory(_regionPathsMemory, regionMemoryKey);

        if (TryGetPathFromMemory(origin, destination, regionMemory, out var knownPath)) {
            return knownPath;
        }

        var maybeNullPath = AStar(
            origin,
            destination,
            (from, to) => from.Center.DistanceTo(to.Center),
            current => current.GetReachableNeighbors().Where(neighbor => !excludedRegions.Contains(neighbor))
        );

        if (maybeNullPath == null) {
            if (excludedRegions.Count == 0) {
                Logger.Info("No path found between {0} and {1}", origin, destination);
            }

            SavePathToMemory(origin, destination, regionMemory, null);
        }
        else {
            // Save all sub paths because they also are the shortest paths
            // This can save a lot of computing if you try to always pathfind the longest expected paths first
            for (var skip = 0; skip <= maybeNullPath.Count - 2; skip++) {
                for (var take = 2; take <= maybeNullPath.Count - skip; take++) {
                    var path = maybeNullPath.Skip(skip).Take(take).ToList();
                    SavePathToMemory(path.First(), path.Last(), regionMemory, path);
                }
            }
        }

        return maybeNullPath;
    }

    /// <summary>
    /// Gets a region memory key based on the blocked regions.
    /// </summary>
    /// <param name="blockedRegions">The blocked regions</param>
    /// <returns>A unique key for a unique set of blocked regions</returns>
    private static string GetRegionMemoryKey(IReadOnlyCollection<IRegion> blockedRegions) {
        if (blockedRegions.Count == 0) {
            return "NO-BLOCKERS";
        }

        // We sort to get a deterministic string
        var sortedRegionStrings = blockedRegions
            .Select(region => region.Center.ToString())
            .OrderBy(posString => posString);

        return string.Join(" ", sortedRegionStrings);
    }

    /// <summary>
    /// Invalidate the pathfinding memory.
    /// This is useful if rocks are broken because new paths might be available.
    /// </summary>
    public void InvalidateCache() {
        CellPathsMemory.Clear();
        _regionPathsMemory.Clear();
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
    /// <param name="getEdgeLength">A function that computes the distance between two vertex.</param>
    /// <param name="getVertexNeighbors">A function that returns the neighbors of a vertex.</param>
    /// <returns>The requested path, or null if the destination is unreachable from the origin.</returns>
    private static List<TVertex> AStar<TVertex>(
        TVertex origin,
        TVertex destination,
        Func<TVertex, TVertex, float> getEdgeLength,
        Func<TVertex, IEnumerable<TVertex>> getVertexNeighbors
    ) {
        var cameFrom = new Dictionary<TVertex, TVertex>();

        var gScore = new Dictionary<TVertex, float>
        {
            [origin] = 0,
        };

        var fScore = new Dictionary<TVertex, float>
        {
            [origin] = getEdgeLength(origin, destination),
        };

        var openSetContents = new HashSet<TVertex>
        {
            origin,
        };
        var openSet = new PriorityQueue<TVertex, float>();
        openSet.Enqueue(origin, fScore[origin]);

        while (openSet.Count > 0) {
            var current = openSet.Dequeue();
            openSetContents.Remove(current);

            if (EqualityComparer<TVertex>.Default.Equals(current, destination)) {
                return BuildPath(cameFrom, current);
            }

            foreach (var neighbor in getVertexNeighbors(current)) {
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

    private static List<TVertex> BuildPath<TVertex>(IReadOnlyDictionary<TVertex, TVertex> cameFrom, TVertex end) {
        var current = end;
        var path = new List<TVertex> { current };
        while (cameFrom.ContainsKey(current)) {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();

        return path;
    }

    private static bool TryGetPathFromMemory<TVertex>(TVertex origin, TVertex destination, IReadOnlyDictionary<TVertex, Dictionary<TVertex, List<TVertex>>> memory, out List<TVertex> path) {
        if (memory.ContainsKey(origin) && memory[origin].ContainsKey(destination)) {
            path = memory[origin][destination];
            return true;
        }

        path = null;
        return false;
    }

    private static void SavePathToMemory<TVertex>(TVertex origin, TVertex destination, IDictionary<TVertex, Dictionary<TVertex, List<TVertex>>> memory, List<TVertex> path) {
        if (!memory.ContainsKey(origin)) {
            memory[origin] = new Dictionary<TVertex, List<TVertex>>();
        }

        if (!memory.ContainsKey(destination)) {
            memory[destination] = new Dictionary<TVertex, List<TVertex>>();
        }

        memory[origin][destination] = path;
        memory[destination][origin] = path == null ? null : Enumerable.Reverse(path).ToList();
    }

    private static Dictionary<IRegion, Dictionary<IRegion, IRegionPath>> GetRegionMemory(IDictionary<string, Dictionary<IRegion, Dictionary<IRegion, IRegionPath>>> memory, string key) {
        if (!memory.ContainsKey(key)) {
            memory[key] = new Dictionary<IRegion, Dictionary<IRegion, IRegionPath>>();
        }

        return memory[key];
    }
}
