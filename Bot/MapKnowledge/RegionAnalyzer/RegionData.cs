using System.Collections.Generic;
using System.Numerics;

namespace Bot.MapKnowledge;

public class RegionData {
    public List<HashSet<Vector2>> Regions { get; }
    public List<HashSet<Vector2>> Ramps { get; }
    public List<Vector2> Noise { get; }
    public List<ChokePoint> ChokePoints { get; }

    public RegionData(List<HashSet<Vector2>> regions, List<HashSet<Vector2>> ramps, List<Vector2> noise, List<ChokePoint> chokePoints) {
        Regions = regions;
        Ramps = ramps;
        Noise = noise;
        ChokePoints = chokePoints;
    }
}
