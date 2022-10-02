using System.Collections.Generic;
using System.Numerics;

namespace Bot.MapKnowledge;

public class NeighboringRegion {
    public Region Region { get; }
    public HashSet<Vector3> Frontier { get; }

    public NeighboringRegion(Region region, HashSet<Vector3> frontier) {
        Region = region;
        Frontier = frontier;
    }
}
