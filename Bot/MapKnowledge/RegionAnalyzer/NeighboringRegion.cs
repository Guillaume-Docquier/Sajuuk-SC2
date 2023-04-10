using System.Collections.Generic;
using System.Numerics;

namespace Bot.MapKnowledge;

public class NeighboringRegion {
    public IRegion Region { get; }
    public HashSet<Vector2> Frontier { get; }

    public NeighboringRegion(IRegion region, HashSet<Vector2> frontier) {
        Region = region;
        Frontier = frontier;
    }
}
