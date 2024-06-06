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
    /// </summary>
    public IEnumerable<INeighboringRegion> Neighbors { get; } // TODO GD Change this to frontier
}
