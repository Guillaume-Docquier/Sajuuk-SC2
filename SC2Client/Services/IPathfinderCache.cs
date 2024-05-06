namespace SC2Client.Services;

/// <summary>
/// A multi layer cache for storing pathfinding paths.
/// A slot key is used to access different cache layers when pathfinding using different constraints.
/// </summary>
/// <typeparam name="TVertex">The type of the vertices.</typeparam>
public interface IPathfinderCache<TVertex> where TVertex : notnull {
    /// <summary>
    /// Gets a path from the cache.
    /// </summary>
    /// <param name="slotKey">The key for the cache slot.</param>
    /// <param name="origin">The origin.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="path">The path that was found.</param>
    /// <returns></returns>
    bool TryGet(string slotKey, TVertex origin, TVertex destination, out List<TVertex>? path);

    /// <summary>
    /// Stores the path in the cache.
    /// </summary>
    /// <param name="slotKey">The key for the cache slot.</param>
    /// <param name="origin">The origin.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="path">The path to store.</param>
    void Save(string slotKey, TVertex origin, TVertex destination, List<TVertex>? path);
}
