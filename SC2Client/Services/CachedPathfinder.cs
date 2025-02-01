using Algorithms;

namespace SC2Client.Services;

/// <summary>
/// A pathfinder that leverages a cache for efficient pathfinding.
/// </summary>
/// <typeparam name="TVertex"></typeparam>
public class CachedPathfinder<TVertex> : IPathfinder<TVertex> where TVertex : notnull {
    private readonly ILogger _logger;
    private readonly IPathfinderCache<TVertex> _cache;

    private readonly Func<TVertex, TVertex, float> _getEdgeLength;
    private readonly Func<TVertex, IEnumerable<TVertex>> _getVertexNeighbors;
    private readonly Func<TVertex, TVertex> _normalizeVertex;
    private readonly Func<TVertex, string> _getVertexId;

    /// <summary>
    /// Creates a pathfinder that uses a cache.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="cache">The cache to use.</param>
    /// <param name="getEdgeLength">A functor that returns the edge length (i.e: the distance) between 2 vertices.</param>
    /// <param name="getVertexNeighbors">A functor that returns the reachable neighbors of a vertex.</param>
    /// <param name="normalizeVertex">A functor that normalizes a vertex so that similar vertices are normalized to the same value. This is very important for efficient caching.</param>
    /// <param name="getVertexId">A function that produces a stable, unique, id for a vertex. This is used for multi layer caching.</param>
    public CachedPathfinder(
        ILogger logger,
        IPathfinderCache<TVertex> cache,
        Func<TVertex, TVertex, float> getEdgeLength,
        Func<TVertex, IEnumerable<TVertex>> getVertexNeighbors,
        Func<TVertex, TVertex> normalizeVertex,
        Func<TVertex, string> getVertexId
    ) {
        _logger = logger.CreateNamed("CachedPathfinder");
        _cache = cache;
        _getEdgeLength = getEdgeLength;
        _getVertexNeighbors = getVertexNeighbors;
        _normalizeVertex = normalizeVertex;
        _getVertexId = getVertexId;
    }

    public List<TVertex>? FindPath(TVertex origin, TVertex destination, HashSet<TVertex>? excludedVertices = null) {
        origin = _normalizeVertex(origin);
        destination = _normalizeVertex(destination);

        if (origin.Equals(destination)) {
            return new List<TVertex>();
        }

        var slotKey = GetSlotKey(excludedVertices);
        if (_cache.TryGet(slotKey, origin, destination, out var knownPath)) {
            return knownPath;
        }

        var maybeNullPath = Pathfinding.AStar(origin, destination, _getEdgeLength, _getVertexNeighbors);
        if (maybeNullPath == null) {
            _logger.Info($"No path found between {origin} and {destination}");
            _cache.Save(slotKey, origin, destination, null);
            return null;
        }

        var path = maybeNullPath.Select(_normalizeVertex).ToList();
        _cache.Save(slotKey, origin, destination, path);
        return path;
    }

    /// <summary>
    /// Gets the slot key based on the excluded vertices.
    /// </summary>
    /// <param name="excludedVertices">The excluded vertices.</param>
    /// <returns></returns>
    private string GetSlotKey(IReadOnlyCollection<TVertex>? excludedVertices = null) {
        if (excludedVertices == null) {
            return "Default";
        }

        // We sort to get a deterministic string
        var slotKey = excludedVertices
            .Select(_getVertexId)
            .OrderBy(id => id);

        return string.Join(" ", slotKey);
    }
}
