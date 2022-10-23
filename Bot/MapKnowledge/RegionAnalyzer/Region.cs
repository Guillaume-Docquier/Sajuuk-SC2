using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Bot.ExtensionMethods;

namespace Bot.MapKnowledge;

public class Region {
    public HashSet<Vector3> Cells { get; }
    public Vector3 Center { get; }
    public RegionType Type { get; }

    [JsonIgnore]
    public ExpandLocation ExpandLocation { get; private set; }

    [JsonIgnore]
    public HashSet<NeighboringRegion> Neighbors { get; private set; }

    [JsonConstructor]
    public Region(HashSet<Vector3> cells, Vector3 center, RegionType type) {
        Cells = cells;
        Center = center;
        Type = type;
    }

    public Region(IEnumerable<Vector2> cells, RegionType type) {
        Cells = cells.Select(vector2 => vector2.ToVector3()).ToHashSet();

        Type = type;
        if (Type == RegionType.Unknown) {
            var expandInRegion = ExpandAnalyzer.ExpandLocations.FirstOrDefault(expandLocation => Cells.Contains(expandLocation.Position));
            if (expandInRegion != default) {
                Type = RegionType.Expand;
                Center = expandInRegion.Position;
            }
            else {
                Type = RegionType.OpenArea;
            }
        }

        if (Center == default) {
            var regionCenter = Clustering.GetCenter(Cells.ToList());
            Center = Cells.MinBy(cell => cell.HorizontalDistanceTo(regionCenter));
        }
    }

    public void Init() {
        var neighbors = new Dictionary<Region, List<Vector3>>();
        foreach (var cell in Cells) {
            var neighboringRegions = cell
                .GetNeighbors()
                .Where(neighbor => neighbor.HorizontalDistanceTo(cell) <= 1) // Disallow diagonals
                .Select(RegionAnalyzer.GetRegion)
                .Where(region => region != null && region != this);

            foreach (var neighboringRegion in neighboringRegions) {
                if (!neighbors.ContainsKey(neighboringRegion)) {
                    neighbors[neighboringRegion] = new List<Vector3> { cell };
                }
                else {
                    neighbors[neighboringRegion].Add(cell);
                }
            }
        }

        Neighbors = new HashSet<NeighboringRegion>();
        foreach (var (region, frontier) in neighbors) {
            Neighbors.Add(new NeighboringRegion(region, frontier.ToHashSet()));
        }

        var expandInRegion = ExpandAnalyzer.ExpandLocations.FirstOrDefault(expandLocation => Cells.Contains(expandLocation.Position));
        ExpandLocation = expandInRegion;
    }
}
