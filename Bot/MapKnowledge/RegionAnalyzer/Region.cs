using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Bot.ExtensionMethods;

namespace Bot.MapKnowledge;

public class Region {
    public HashSet<Vector3> Cells { get; }
    public Vector3 Center { get; }

    [JsonIgnore]
    public HashSet<Vector3> Frontier { get; private set; }

    [JsonIgnore]
    public HashSet<Region> Neighbors { get; private set; }

    [JsonConstructor]
    public Region(HashSet<Vector3> cells, Vector3 center) {
        Cells = cells;
        Center = center;
    }

    public Region(IEnumerable<Vector2> cells) {
        Cells = cells.Select(vector2 => vector2.ToVector3().WithWorldHeight()).ToHashSet();

        var regionCenter = Clustering.GetCenter(Cells.ToList());
        Center = Cells.MinBy(cell => cell.HorizontalDistanceTo(regionCenter));
    }

    public void SetFrontiersAndNeighbors() {
        var frontierCells = Cells.Where(
            cell => cell
                .GetNeighbors()
                .Where(neighbor => neighbor.HorizontalDistanceTo(cell) <= 1) // Disallow diagonals
                .Any(neighbor => {
                    var region = RegionAnalyzer.GetRegion(neighbor);
                    return region != null && region != this;
                })
        );

        Frontier = frontierCells.ToHashSet();

        Neighbors = Frontier.SelectMany(cell => cell
            .GetNeighbors()
            .Select(RegionAnalyzer.GetRegion)
            .Where(region => region != null && region != this)
        ).ToHashSet();
    }
}
