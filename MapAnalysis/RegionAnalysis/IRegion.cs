using System.Numerics;
using MapAnalysis.ExpandAnalysis;
using SC2APIProtocol;

namespace MapAnalysis.RegionAnalysis;

public interface IRegion {
    /// <summary>
    /// The unique id of the region
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// A color for this region
    /// </summary>
    public Color Color { get; }

    /// <summary>
    /// A cells that's considered the central point of the region.
    /// For regions that contain an expand, the center is the expand location.
    /// </summary>
    public Vector2 Center { get; }

    /// <summary>
    /// Contains all walkable world grid center cells in this region.
    /// </summary>
    public HashSet<Vector2> Cells { get; }

    /// <summary>
    /// The type of this region.
    /// </summary>
    public RegionType Type { get; }

    /// <summary>
    /// If the type of the region is RegionType.Expand, ExpandLocation will be the expand location of the region.
    /// </summary>
    public IExpandLocation? ExpandLocation { get; }

    /// <summary>
    /// Represents the neighbors of this region. A neighbor is described as another Region and its frontier.
    /// TODO GD Change this to frontier with obstructions. We're not interested in a region being obstructed, but rather if you can go from one region to another.
    /// TODO GD Sometimes, going to B from A won't be possible, gut going to C from A will be. That's because the frontier is obstructed, not the region.
    /// </summary>
    public IEnumerable<INeighboringRegion> Neighbors { get; }

    /// <summary>
    /// Whether this region is currently obstructed
    /// </summary>
    [Obsolete("Will be removed in favor of frontier obstruction")]
    public bool IsObstructed { get; }

    /// <summary>
    /// Gets all the regions that can be reached from this region.
    /// </summary>
    /// <returns></returns>
    [Obsolete("Will be removed in favor of frontier obstruction")]
    public IEnumerable<IRegion> GetReachableNeighbors();
}
