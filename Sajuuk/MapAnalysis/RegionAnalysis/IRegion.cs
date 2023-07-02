using System;
using System.Collections.Generic;
using System.Numerics;
using Sajuuk.MapAnalysis.ExpandAnalysis;
using SC2APIProtocol;

namespace Sajuuk.MapAnalysis.RegionAnalysis;

// TODO GD This interface is rather "fat", but changing it represents a lot changes.
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
    /// An approximation of the radius of the region.
    /// TODO GD Get rid of this
    /// </summary>
    [Obsolete("Don't use ApproximatedRadius in new code")]
    public float ApproximatedRadius { get; }

    /// <summary>
    /// The type of this region.
    /// </summary>
    public RegionType Type { get; }

    /// <summary>
    /// If the type of the region is RegionType.Expand, ExpandLocation will be the expand location of the region.
    /// </summary>
    public IExpandLocation ExpandLocation { get; }

    /// <summary>
    /// Represents the neighbors of this region. A neighbor is describes as another Region and its frontier.
    /// </summary>
    public IEnumerable<INeighboringRegion> Neighbors { get; }

    /// <summary>
    /// Whether the region is obstructed and the passage from one frontier to another is impossible.
    /// </summary>
    public bool IsObstructed { get; }

    /// <summary>
    /// Gets all the neighboring regions that can be reached.
    /// </summary>
    /// <returns>A list of regions that are accessible from this region.</returns>
    public IEnumerable<IRegion> GetReachableNeighbors();
}
