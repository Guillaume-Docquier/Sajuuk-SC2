using System.Collections.Generic;
using System.Numerics;

namespace Bot.MapKnowledge;

public class NeighboringRegion {
    public Region Region { get; }
    public HashSet<Vector2> Frontier { get; }

    public NeighboringRegion(Region region, HashSet<Vector2> frontier) {
        Region = region;
        Frontier = frontier;
    }
}
