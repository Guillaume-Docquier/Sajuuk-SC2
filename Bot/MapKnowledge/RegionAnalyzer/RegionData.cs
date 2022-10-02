using System.Collections.Generic;
using System.Numerics;

namespace Bot.MapKnowledge;

public class RegionData {
    public List<Region> Regions { get; }
    public List<HashSet<Vector2>> Ramps { get; }
    public List<Vector2> Noise { get; }
    public List<ChokePoint> ChokePoints { get; }

    public RegionData(List<Region> regions, List<HashSet<Vector2>> ramps, List<Vector2> noise, List<ChokePoint> chokePoints) {
        Regions = regions;
        Ramps = ramps;
        Noise = noise;
        ChokePoints = chokePoints;
    }
}
