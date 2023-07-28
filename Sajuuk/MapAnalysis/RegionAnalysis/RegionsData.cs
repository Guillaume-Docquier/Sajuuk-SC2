using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Sajuuk.MapAnalysis.RegionAnalysis.ChokePoints;

namespace Sajuuk.MapAnalysis.RegionAnalysis;

public class RegionsData {
    [JsonInclude] public List<Region> Regions { get; private set; }
    [JsonInclude] public List<HashSet<Vector2>> Ramps { get; private set; }
    [JsonInclude] public List<Vector2> Noise { get; private set; }
    [JsonInclude] public List<ChokePoint> ChokePoints { get; private set; }

    [JsonConstructor]
    [Obsolete("Do not use this parameterless JsonConstructor", error: true)]
    public RegionsData() {}

    public RegionsData(IEnumerable<Region> regions, IEnumerable<HashSet<Vector2>> ramps, IEnumerable<Vector2> noise, IEnumerable<ChokePoint> chokePoints) {
        // We sort the collections to have deterministic structures.
        Regions = regions
            .OrderBy(region => region.Id)
            .ToList();

        Ramps = ramps
            .Select(set => set.OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToHashSet())
            .OrderBy(set => GetCenter(set).Y)
            .ThenBy(set => GetCenter(set).X)
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

    private static Vector2 GetCenter(IReadOnlyCollection<Vector2> cluster) {
        var avgX = cluster.Average(position => position.X);
        var avgY = cluster.Average(position => position.Y);

        return new Vector2(avgX, avgY);
    }
}
