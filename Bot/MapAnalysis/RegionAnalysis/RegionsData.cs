using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;
using Bot.MapAnalysis.RegionAnalysis.ChokePoints;

namespace Bot.MapAnalysis.RegionAnalysis;

public class RegionsData {
    [JsonInclude] public List<Region> Regions { get; private set; }
    [JsonInclude] public List<HashSet<Vector2>> Ramps { get; private set; }
    [JsonInclude] public List<Vector2> Noise { get; private set; }
    [JsonInclude] public List<ChokePoint> ChokePoints { get; private set; }

    [JsonConstructor]
    [Obsolete("Do not use this parameterless JsonConstructor", error: true)]
    public RegionsData() {}

    public RegionsData(List<Region> regions, List<HashSet<Vector2>> ramps, List<Vector2> noise, List<ChokePoint> chokePoints) {
        Regions = regions;
        Ramps = ramps;
        Noise = noise;
        ChokePoints = chokePoints;
    }
}
