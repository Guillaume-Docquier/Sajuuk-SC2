using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Bot.MapAnalysis.RegionAnalysis;

public class NeighboringRegion : INeighboringRegion {
    [JsonInclude] public Region ConcreteRegion;
    [JsonIgnore] public IRegion Region => ConcreteRegion;
    [JsonInclude] public HashSet<Vector2> Frontier { get; set; }

    [JsonConstructor]
    public NeighboringRegion(Region concreteRegion, HashSet<Vector2> frontier) {
        ConcreteRegion = concreteRegion;
        Frontier = frontier;
    }
}
