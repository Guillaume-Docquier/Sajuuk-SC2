using System.Numerics;
using System.Text.Json.Serialization;
using Algorithms;
using MapAnalysis.RegionAnalysis.ChokePoints;
using MapAnalysis.RegionAnalysis.Ramps;

namespace MapAnalysis.RegionAnalysis;

public class RegionsData : IRegionsData {
    [JsonInclude] public List<IRegion> Regions { get; private set; }
    [JsonInclude] public List<HashSet<Vector2>> Ramps { get; private set; }
    [JsonInclude] public List<Vector2> Noise { get; private set; }
    [JsonInclude] public List<ChokePoint> ChokePoints { get; private set; }

    [Obsolete("Do not use this parameterless JsonConstructor", error: true)]
    [JsonConstructor] public RegionsData() {}

    public RegionsData(IEnumerable<IRegion> regions, IEnumerable<Ramp> ramps, IEnumerable<Vector2> noise, IEnumerable<ChokePoint> chokePoints) {
        // We sort the collections to have deterministic structures.
        Regions = regions
            .OrderBy(region => region.Id)
            .ToList();

        Ramps = ramps
            .Select(ramp => ramp.Cells.OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToHashSet())
            .OrderBy(set => Clustering.GetCenter(set).Y)
            .ThenBy(set => Clustering.GetCenter(set).X)
            .ToList();

        Noise = noise
            .OrderBy(cell => cell.Y)
            .ThenBy(cell => cell.X)
            .ToList();

        ChokePoints = chokePoints
            .OrderBy(chokePoint => chokePoint.Start.Y)
            .ThenBy(chokePoint => chokePoint.Start.X)
            .ToList();
    }
}
