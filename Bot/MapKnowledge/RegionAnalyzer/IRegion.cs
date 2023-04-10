using System.Collections.Generic;
using System.Numerics;
using SC2APIProtocol;

namespace Bot.MapKnowledge;

// TODO GD This interface is rather "fat", but changing it represents a lot changes.
public interface IRegion {
    public int Id { get; }
    public Color Color { get; }

    public Vector2 Center { get; }
    public HashSet<Vector2> Cells { get; }
    public float ApproximatedRadius { get; }

    public RegionType Type { get; }
    public ExpandLocation ExpandLocation { get; }
    public HashSet<NeighboringRegion> Neighbors { get; }
    public bool IsObstructed { get; }

    public IEnumerable<IRegion> GetReachableNeighbors();
    public void UpdateObstruction();
}
