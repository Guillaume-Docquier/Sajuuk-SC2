using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;
using Bot.MapAnalysis.RegionAnalysis.ChokePoints;

namespace Bot.MapAnalysis.RegionAnalysis;

public class RegionsData {
    public List<Region> Regions { get; }
    public List<HashSet<Vector2>> Ramps { get; }
    public List<Vector2> Noise { get; }
    public List<ChokePoint> ChokePoints { get; }

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
