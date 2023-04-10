using System.Collections.Generic;
using System.Numerics;
using SC2APIProtocol;

namespace Bot.MapKnowledge;

// TODO GD This interface is rather "fat", but changing it means a lot changes.
public interface IRegion {
    public int Id { get; }
    public Color Color { get; }

    Vector2 Center { get; }
    public HashSet<Vector2> Cells { get; }
    public float ApproximatedRadius { get; }

    RegionType Type { get; }
    public ExpandLocation ExpandLocation { get; }
    public HashSet<NeighboringRegion> Neighbors { get; }

    bool IsObstructed { get; }

    public IEnumerable<IRegion> GetReachableNeighbors();
    public void UpdateObstruction();
}
