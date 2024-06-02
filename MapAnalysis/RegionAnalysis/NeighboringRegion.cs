using System.Numerics;
using System.Text.Json.Serialization;

namespace MapAnalysis.RegionAnalysis;

public class NeighboringRegion : INeighboringRegion {
    [JsonInclude] public Region ConcreteRegion;
    [JsonIgnore] public IRegion Region => ConcreteRegion;

    [JsonInclude] public HashSet<Vector2> Frontier { get; set; }

    [JsonConstructor]
    public NeighboringRegion() {}

    public NeighboringRegion(Region concreteRegion, HashSet<Vector2> frontier) {
        ConcreteRegion = concreteRegion;
        Frontier = frontier;
    }
}
