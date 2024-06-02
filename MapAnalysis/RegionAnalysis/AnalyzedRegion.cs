using System.Numerics;
using SC2APIProtocol;

namespace MapAnalysis.RegionAnalysis;

public class AnalyzedRegion : Region {
    private static readonly Color RampColor = Colors.Cyan;
    private static readonly List<Color> RegionColors = new List<Color>
    {
        Colors.Magenta,
        Colors.Orange,
        Colors.Blue,
        Colors.Red,
        Colors.LimeGreen,
    };

    public AnalyzedRegion(
        ITerrainTracker terrainTracker,
        IClustering clustering,
        IPathfinder pathfinder,
        IUnitsTracker unitsTracker,
        IEnumerable<Vector2> cells,
        RegionType type,
        IEnumerable<ExpandLocation> expandLocations
    ) : base(terrainTracker, clustering, pathfinder, unitsTracker) {
        // We order the cells to have a deterministic structure when persisting.
        // When enumerated, hashsets keep the insertion order.
        Cells = cells
            .OrderBy(cell => cell.X)
            .ThenBy(cell => cell.Y)
            .ToHashSet();

        // The approximated radius is the diagonal of the cells as if they were a square
        // We also scale them by ^1.05 because this estimation tends to be worse for large regions
        // Using an exponent makes it so large regions will get a higher scaling
        var squareSide = Math.Sqrt(Cells.Count);
        var squareDiagonal = Math.Sqrt(2) * squareSide;
        ApproximatedRadius = (float)Math.Pow(squareDiagonal / 2, 1.05);

        Type = type;
        if (Type == RegionType.Unknown) {
            var expandInRegion = expandLocations.FirstOrDefault(expandLocation => Cells.Contains(expandLocation.Position));
            if (expandInRegion != default) {
                Type = RegionType.Expand;
                Center = expandInRegion.Position;
                ConcreteExpandLocation = expandInRegion;
                expandInRegion.Region = this;
            }
            else {
                Type = RegionType.OpenArea;
            }
        }

        Color = Type == RegionType.Ramp
            ? RampColor
            : RegionColors.First();

        if (Center == default) {
            var regionCenter = clustering.GetCenter(Cells.ToList());
            Center = Cells.MinBy(cell => cell.DistanceTo(regionCenter));
        }
    }

    public void FinalizeCreation(int id, IEnumerable<AnalyzedRegion> allRegions) {
        Id = id;

        ConcreteNeighbors = ComputeNeighboringRegions(Cells, allRegions.Where(region => region != this).ToHashSet())
            .OrderBy(neighbor => neighbor.Region.Id)
            .ToHashSet();

        IsObstructed = IsRegionObstructed();
        Color = GetDifferentColorFromNeighbors();
    }

    private static HashSet<NeighboringRegion> ComputeNeighboringRegions(IEnumerable<Vector2> cells, IReadOnlyCollection<AnalyzedRegion> allOtherRegions) {
        var neighborsMap = new Dictionary<AnalyzedRegion, NeighboringRegion>();
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
