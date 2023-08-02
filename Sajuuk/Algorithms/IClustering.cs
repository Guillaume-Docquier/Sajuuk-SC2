using System.Collections.Generic;
using System.Numerics;

namespace Sajuuk.Algorithms;

public interface IClustering {
    /// <summary>
    /// Performs a flood fill on the given cells, starting from the provided starting point.
    /// </summary>
    /// <param name="cells"></param>
    /// <param name="startingPoint"></param>
    /// <returns>The cells reached by the flood fill</returns>
    IEnumerable<Vector2> FloodFill(IReadOnlySet<Vector2> cells, Vector2 startingPoint);

    /// <summary>
    /// <para>A textbook implementation of the DBSCAN clustering algorithm.</para>
    /// <para>See https://en.wikipedia.org/wiki/DBSCAN</para>
    /// </summary>
    /// <param name="positions">The positions to cluster</param>
    /// <param name="epsilon">How close a point needs to be to be considered nearby</param>
    /// <param name="minPoints">How many points need to be nearby to count as a cluster node</param>
    /// <returns>A list of clusters and the resulting noise</returns>
    (List<List<Vector2>> clusters, List<Vector2> noise) DBSCAN(List<Vector2> positions, float epsilon, int minPoints);

    /// <summary>
    /// <para>A textbook implementation of the DBSCAN clustering algorithm.</para>
    /// <para>See https://en.wikipedia.org/wiki/DBSCAN</para>
    /// </summary>
    /// <param name="items">The IHavePosition items to cluster</param>
    /// <param name="epsilon">How close an item needs to be to be considered nearby</param>
    /// <param name="minPoints">How many items need to be nearby to count as a cluster node</param>
    /// <returns>A list of clusters and the resulting noise</returns>
    (List<List<T>> clusters, List<T> noise) DBSCAN<T>(IReadOnlyCollection<T> items, float epsilon, int minPoints) where T: class, IHavePosition;

    Vector2 GetCenter<T>(IEnumerable<T> cluster) where T: class, IHavePosition;
    Vector2 GetCenter(List<Vector2> cluster);
    Vector3 GetBoundingBoxCenter<T>(IEnumerable<T> cluster) where T: class, IHavePosition;
    Vector3 GetBoundingBoxCenter(List<Vector2> cluster);
}
