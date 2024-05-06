namespace SC2Client.Services;

/// <summary>
/// A pathfinder to find the shortest path between two vertices.
/// </summary>
/// <typeparam name="TVertex">The type of the vertices.</typeparam>
public interface IPathfinder<TVertex> where TVertex: notnull {
    /// <summary>
    /// <para>Finds a path between the origin and destination.</para>
    /// <para>The pathing considers rocks but not buildings or units.</para>
    /// <para>The results are cached so subsequent calls with the same origin and destinations are free.</para>
    /// </summary>
    /// <param name="origin">The origin position.</param>
    /// <param name="destination">The destination position.</param>
    /// <param name="excludedVertices">The vertices that cannot be used for pathfinding.</param>
    /// <returns>The requested path, or null if the destination is unreachable from the origin.</returns>
    List<TVertex>? FindPath(TVertex origin, TVertex destination, HashSet<TVertex>? excludedVertices = null);
}
