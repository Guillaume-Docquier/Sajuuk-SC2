using System.Collections.Generic;
using System.Numerics;

namespace Sajuuk.MapAnalysis.RegionAnalysis;

// TODO GD Change this to be a "frontier" instead and add if the frontier is blocked (also add fromRegion and toRegion)
public interface INeighboringRegion {
    public IRegion Region { get; }
    public HashSet<Vector2> Frontier { get; }
}
