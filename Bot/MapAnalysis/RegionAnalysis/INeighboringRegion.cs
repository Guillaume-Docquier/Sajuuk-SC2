using System.Collections.Generic;
using System.Numerics;

namespace Bot.MapAnalysis.RegionAnalysis;

public interface INeighboringRegion {
    public IRegion Region { get; }
    public HashSet<Vector2> Frontier { get; }
}
