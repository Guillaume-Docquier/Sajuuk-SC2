using System.Numerics;
using System.Text.Json.Serialization;
using Algorithms;
using Algorithms.ExtensionMethods;
using MapAnalysis.ExpandAnalysis;
using SC2APIProtocol;
using SC2Client.Debugging.GraphicalDebugging;
using SC2Client.ExtensionMethods;

namespace MapAnalysis.RegionAnalysis;

public class Region : IRegion {
    private static readonly Color RampColor = Colors.Cyan;
    private static readonly List<Color> RegionColors = new List<Color>
    {
        Colors.Magenta,
        Colors.Orange,
        Colors.Blue,
        Colors.Red,
        Colors.LimeGreen,
    };

    [JsonInclude] public int Id { get; set; }
    [JsonInclude] public Color Color { get; set; }
    [JsonInclude] public Vector2 Center { get; set; }
    [JsonInclude] public HashSet<Vector2> Cells { get; }
    [JsonInclude] public RegionType Type { get; set; }
    [JsonInclude] public IExpandLocation? ExpandLocation { get; }
    [JsonInclude] public IEnumerable<INeighboringRegion> Neighbors { get; private set; }

    [Obsolete("Do not use this parameterless JsonConstructor", error: true)]
    [JsonConstructor] public Region() {}

    public Region(
        IEnumerable<Vector2> cells,
        RegionType type,
        IEnumerable<IExpandLocation> expandLocations
    ) {
        // We order the cells to have a deterministic structure when persisting.
        // When enumerated, hashsets keep the insertion order.
        Cells = cells
            .OrderBy(cell => cell.X)
            .ThenBy(cell => cell.Y)
            .ToHashSet();

        Type = type;
        if (Type == RegionType.Unknown) {
            var expandInRegion = expandLocations.FirstOrDefault(expandLocation => Cells.Contains(expandLocation.OptimalTownHallPosition));
            if (expandInRegion != default) {
                Type = RegionType.Expand;
                Center = expandInRegion.OptimalTownHallPosition;
                ExpandLocation = expandInRegion;
            }
            else {
                Type = RegionType.OpenArea;
            }
        }

        Color = Type == RegionType.Ramp
            ? RampColor
            : RegionColors.First();

        if (Center == default) {
            var regionCenter = Clustering.GetCenter(Cells.ToList());
            Center = Cells.MinBy(cell => cell.DistanceTo(regionCenter));
        }
    }

    // TODO GD Do I really need this?
    public void FinalizeCreation(int id, IEnumerable<Region> allRegions) {
        Id = id;

        Neighbors = ComputeNeighboringRegions(Cells, allRegions.Where(region => region != this).ToHashSet())
            .OrderBy(neighbor => neighbor.Region.Id)
            .ToHashSet();

        Color = GetDifferentColorFromNeighbors();
    }

    private static HashSet<NeighboringRegion> ComputeNeighboringRegions(IEnumerable<Vector2> cells, IReadOnlyCollection<Region> allOtherRegions) {
        var neighborsMap = new Dictionary<Region, NeighboringRegion>();
        foreach (var cell in cells) {
            var neighboringRegions = cell
                .GetNeighbors()
                .Where(neighbor => neighbor.DistanceTo(cell) <= 1) // Disallow diagonals
                .Select(neighbor => allOtherRegions.FirstOrDefault(region => region.Cells.Contains(neighbor)))
                .Where(region => region != null);

            foreach (var neighboringRegion in neighboringRegions) {
                if (!neighborsMap.ContainsKey(neighboringRegion)) {
                    neighborsMap[neighboringRegion] = new NeighboringRegion(neighboringRegion, new HashSet<Vector2> { cell });
                }
                else {
                    neighborsMap[neighboringRegion].Frontier.Add(cell);
                }
            }
        }

        return neighborsMap.Values.ToHashSet();
    }

    /// <summary>
    /// Gets a color that's different from the neighbors colors.
    /// </summary>
    /// <returns>A color that's different from the colors of all neighbors.</returns>
    private Color GetDifferentColorFromNeighbors() {
        var neighborColors = Neighbors.Select(neighbor => neighbor.Region.Color).ToHashSet();
        if (!neighborColors.Contains(Color)) {
            return Color;
        }

        // There should be enough colors so that one is always available
        var distinctRegionColor = RegionColors
            .Except(neighborColors)
            .Take(1)
            .First();

        return distinctRegionColor;
    }
}
