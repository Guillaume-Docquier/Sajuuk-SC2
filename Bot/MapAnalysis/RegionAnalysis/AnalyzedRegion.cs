using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Algorithms;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.MapAnalysis.ExpandAnalysis;
using SC2APIProtocol;

namespace Bot.MapAnalysis.RegionAnalysis;

public class AnalyzedRegion : Region {
    private static readonly List<Color> RegionColors = new List<Color>
    {
        Colors.Cyan,
        Colors.Red,
        Colors.LimeGreen,
        Colors.Blue,
        Colors.Orange,
        Colors.Magenta
    };

    public AnalyzedRegion(IEnumerable<Vector2> cells, RegionType type) {
        Cells = cells.ToHashSet();

        // The approximated radius is the diagonal of the cells as if they were a square
        // We also scale them by ^1.05 because this estimation tends to be worse for large regions
        // Using an exponent makes it so large regions will get a higher scaling
        var squareSide = Math.Sqrt(Cells.Count);
        var squareDiagonal = Math.Sqrt(2) * squareSide;
        ApproximatedRadius = (float)Math.Pow(squareDiagonal / 2, 1.05);

        Type = type;
        if (Type == RegionType.Unknown) {
            var expandInRegion = ExpandAnalyzer.Instance.ExpandLocations.FirstOrDefault(expandLocation => Cells.Contains(expandLocation.Position));
            if (expandInRegion != default) {
                Type = RegionType.Expand;
                Center = expandInRegion.Position;
            }
            else {
                Type = RegionType.OpenArea;
            }
        }

        if (Center == default) {
            var regionCenter = Clustering.Instance.GetCenter(Cells.ToList());
            Center = Cells.MinBy(cell => cell.DistanceTo(regionCenter));
        }
    }

    public void FinalizeCreation(int id, IEnumerable<AnalyzedRegion> allRegions) {
        Id = id;
        ConcreteNeighbors = ComputeNeighboringRegions(Cells, allRegions.Where(region => region != this).ToHashSet());
        IsObstructed = IsRegionObstructed();
        Color = ComputeDistinctColor();
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

    private Color ComputeDistinctColor() {
        // Get a random color that's not the same as our neighbors
        var rng = new Random();
        var color = RegionColors[rng.Next(RegionColors.Count)];
        var neighborColors = GetReachableNeighbors()
            .Where(neighbor => neighbor.Color != default)
            .Select(neighbor => neighbor.Color)
            .ToHashSet();

        if (neighborColors.Contains(color)) {
            // There should be enough colors so that one is always available
            var availableColors = RegionColors.Except(neighborColors).ToList();

            var randomColorIndex = rng.Next(availableColors.Count);
            color = availableColors[randomColorIndex];
        }

        return color;
    }
}
