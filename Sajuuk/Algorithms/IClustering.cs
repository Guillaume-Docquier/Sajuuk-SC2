using System.Collections.Generic;
using System.Numerics;

namespace Sajuuk.Algorithms;

public interface IClustering {
    /// <summary>
    /// Performs a flood fill on the given cells, starting from the provided starting point.
    /// </summary>
    /// <param name="cells"></param>
    /// <param name="startingPoint"></param>
    /// <returns>The cells reached by the flood fill.</returns>
    IEnumerable<Vector2> FloodFill(IReadOnlySet<Vector2> cells, Vector2 startingPoint);

    /// <summary>
    /// <para>A textbook implementation of the DBSCAN clustering algorithm that works with Vector2.</para>
    /// <para>See https://en.wikipedia.org/wiki/DBSCAN</para>
    /// </summary>
    /// <param name="positions">The collection of Vector2 to cluster.</param>
    /// <param name="epsilon">How close a point needs to be to be considered nearby.</param>
    /// <param name="minPoints">How many points need to be nearby to count as a cluster node.</param>
    /// <returns>A list of clusters and the resulting noise.</returns>
    // ReSharper disable once InconsistentNaming
    (List<List<Vector2>> clusters, List<Vector2> noise) DBSCAN(IReadOnlyCollection<Vector2> positions, float epsilon, int minPoints);

    /// <summary>
    /// <para>A textbook implementation of the DBSCAN clustering algorithm that works with Vector3.</para>
    /// <para>See https://en.wikipedia.org/wiki/DBSCAN.</para>
    /// </summary>
    /// <param name="positions">The collection of Vector3 to cluster.</param>
    /// <param name="epsilon">How close a point needs to be to be considered nearby.</param>
    /// <param name="minPoints">How many points need to be nearby to count as a cluster node.</param>
    /// <returns>A list of clusters and the resulting noise.</returns>
    // ReSharper disable once InconsistentNaming
    (List<List<Vector3>> clusters, List<Vector3> noise) DBSCAN(IReadOnlyCollection<Vector3> positions, float epsilon, int minPoints);

    /// <summary>
    /// <para>A textbook implementation of the DBSCAN clustering algorithm that works with any object that has a position.</para>
    /// <para>See https://en.wikipedia.org/wiki/DBSCAN.</para>
    /// </summary>
    /// <param name="items">The IHavePosition items to cluster.</param>
    /// <param name="epsilon">How close an item needs to be to be considered nearby.</param>
    /// <param name="minPoints">How many items need to be nearby to count as a cluster node.</param>
    /// <returns>A list of clusters and the resulting noise.</returns>
    // ReSharper disable once InconsistentNaming
    (List<List<T>> clusters, List<T> noise) DBSCAN<T>(IReadOnlyCollection<T> items, float epsilon, int minPoints) where T: class, IHavePosition;

    /// <summary>
    /// Gets the center of mass of the given list of IHavePosition.
    /// </summary>
    /// <param name="items">The items to find the center of.</param>
    /// <returns>A vector2 that represents the center of mass of the items.</returns>
    Vector2 GetCenter(IEnumerable<IHavePosition> items);

    /// <summary>
    /// Gets the center of mass of the given list of cells.
    /// </summary>
    /// <param name="cells">The cells to find the center of.</param>
    /// <returns>A vector2 that represents the center of mass of the cells.</returns>
    Vector2 GetCenter(List<Vector2> cells);

    Vector3 GetBoundingBoxCenter<T>(IEnumerable<T> cluster) where T: class, IHavePosition;
    Vector3 GetBoundingBoxCenter(List<Vector2> cluster);
}
