using System.Numerics;
using MapAnalysis.RegionAnalysis.ChokePoints;

namespace MapAnalysis.RegionAnalysis;

/// <summary>
/// Represents all the data about the region analysis of a single map.
/// </summary>
public interface IRegionsData {
    /// <summary>
    /// All the regions in the map.
    /// </summary>
    List<Region> Regions { get; }

    /// <summary>
    /// All the ramps in the map.
    /// </summary>
    List<HashSet<Vector2>> Ramps { get; }

    /// <summary>
    /// Any cell considered as noise and not included in any region.
    /// </summary>
    List<Vector2> Noise { get; }

    /// <summary>
    /// All the choke points in the map.
    /// </summary>
    List<ChokePoint> ChokePoints { get; }
}
