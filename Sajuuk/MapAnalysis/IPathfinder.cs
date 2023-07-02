using System.Collections.Generic;
using System.Numerics;
using Sajuuk.MapAnalysis.RegionAnalysis;

namespace Sajuuk.MapAnalysis;

public interface IPathfinder {
    /// <summary>
    /// <para>Finds a path between the origin and destination.</para>
    /// <para>The pathing considers rocks but not buildings or units.</para>
    /// <para>The results are cached so subsequent calls with the same origin and destinations are free.</para>
    /// </summary>
    /// <param name="origin">The origin position.</param>
    /// <param name="destination">The destination position.</param>
    /// <returns>The requested path, or null if the destination is unreachable from the origin.</returns>
    List<Vector2> FindPath(Vector2 origin, Vector2 destination);

    /// <summary>
    /// <para>Finds a path between the origin region and the destination region.</para>
    /// <para>The pathing considers rocks but not buildings or units.</para>
    /// <para>The results are cached so subsequent calls with the same origin and destinations are free.</para>
    /// </summary>
    /// <param name="origin">The origin region.</param>
    /// <param name="destination">The destination region.</param>
    /// <param name="excludedRegions">Regions that should be omitted from pathfinding</param>
    /// <returns>The requested path, or null if the destination is unreachable from the origin.</returns>
    List<IRegion> FindPath(IRegion origin, IRegion destination, HashSet<IRegion> excludedRegions = null);

    // TODO GD Get rid of this
    public Dictionary<Vector2, Dictionary<Vector2, List<Vector2>>> CellPathsMemory { get; }
}
