using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using SC2APIProtocol;

namespace Bot.MapKnowledge;

public class Region {
    private static readonly List<Color> RegionColors = new List<Color>
    {
        Colors.Cyan,
        Colors.Red,
        Colors.LimeGreen,
        Colors.Blue,
        Colors.Orange,
        Colors.Magenta
    };

    /// <summary>
    /// Contains all walkable world grid center cells in this region.
    /// </summary>
    public HashSet<Vector2> Cells { get; }

    /// <summary>
    /// The region's center.
    /// It is guaranteed to be a walkable world grid center cell.
    /// </summary>
    public Vector2 Center { get; }
    public RegionType Type { get; }
    public bool IsObstructed { get; private set; }

    // TODO GD Persist this next time you run the algo
    [JsonIgnore]
    public int Id { get; private set; }

    // TODO GD Persist this next time you run the algo
    [JsonIgnore]
    public Color Color { get; private set; }

    [JsonIgnore]
    public ExpandLocation ExpandLocation { get; private set; }

    [JsonIgnore]
    public HashSet<NeighboringRegion> Neighbors { get; private set; }

    /// <summary>
    /// Approximates the region's radius based on its cells
    /// This is a placeholder while we have a smarter region defense behaviour
    /// </summary>
    /// <returns>The approximated region's radius</returns>
    [JsonIgnore]
    public float ApproximatedRadius { get; }

    [JsonConstructor]
    public Region(HashSet<Vector2> cells, Vector2 center, RegionType type, bool isObstructed) {
        Cells = cells;

        // The approximated radius is the diagonal of the cells as if they were a square
        // We also scale them by ^1.05 because this estimation tends to be worse for large regions
        // Using an exponent makes it so large regions will get a higher scaling
        var squareSide = Math.Sqrt(Cells.Count);
        var squareDiagonal = Math.Sqrt(2) * squareSide;
        ApproximatedRadius = (float)Math.Pow(squareDiagonal / 2, 1.05);

        Center = center;
        Type = type;
        IsObstructed = isObstructed;
    }

    public Region(IEnumerable<Vector2> cells, RegionType type) {
        Cells = cells.ToHashSet();
        ApproximatedRadius = (float)Math.Sqrt(Cells.Count) / 2;

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
            Center = Cells.MinBy(cell => cell.DistanceTo(regionCenter));
        }
    }

    public void Init(int id, bool computeObstruction) {
        Id = id;

        var neighbors = new Dictionary<Region, List<Vector2>>();
        foreach (var cell in Cells) {
            var neighboringRegions = cell
                .GetNeighbors()
                .Where(neighbor => neighbor.DistanceTo(cell) <= 1) // Disallow diagonals
                .Select(RegionAnalyzer.GetRegion)
                .Where(region => region != null && region != this);

            foreach (var neighboringRegion in neighboringRegions) {
                if (!neighbors.ContainsKey(neighboringRegion)) {
                    neighbors[neighboringRegion] = new List<Vector2> { cell };
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

        if (computeObstruction) {
            IsObstructed = IsRegionObstructed();
        }

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

        Color = color;
    }

    /// <summary>
    /// Check if the region is still obstructed.
    /// You should call this when neutral units in this region die because we only track those.
    /// </summary>
    public void UpdateObstruction() {
        if (IsObstructed) {
            IsObstructed = IsRegionObstructed();
        }
    }

    /// <summary>
    /// Gets the neighboring regions that can be reached from this region
    /// </summary>
    /// <returns>The neighboring regions that can be reached from this region</returns>
    public IEnumerable<Region> GetReachableNeighbors() {
        return Neighbors
            .Select(neighbor => neighbor.Region)
            .Where(neighbor => !neighbor.IsObstructed);
    }

    private bool IsRegionObstructed() {
        // I **think** only ramps can be obstructed
        if (Type != RegionType.Ramp) {
            return false;
        }

        if (!Cells.Any(cell => MapAnalyzer.IsWalkable(cell))) {
            return true;
        }

        var frontier = Neighbors.SelectMany(neighbor => neighbor.Frontier).ToList();
        var clusteringResult = Clustering.DBSCAN(frontier, epsilon: (float)Math.Sqrt(2), minPoints: 1);
        if (clusteringResult.clusters.Count != 2) {
            Logger.Error("This ramp has {0} frontiers instead of the expected 2", clusteringResult.clusters.Count);
            return false;
        }

        var pathThrough = Pathfinder.FindPath(
            GetWalkableCellNearFrontier(clusteringResult.clusters[0]),
            GetWalkableCellNearFrontier(clusteringResult.clusters[1])
        );

        if (pathThrough == null) {
            return true;
        }

        // Obstructed if the path between the neighbors does not go through this region
        // Let's hope no regions have two direct paths between each other
        var portionPassingThrough = (float)pathThrough.Count(cell => Cells.Contains(cell)) / pathThrough.Count;
        return portionPassingThrough <= 0.5f;
    }

    private static Vector2 GetWalkableCellNearFrontier(IReadOnlyCollection<Vector2> frontier) {
        var walkableCell = frontier.FirstOrDefault(cell => MapAnalyzer.IsWalkable(cell));

        return walkableCell != default
            ? walkableCell
            : frontier.First().ClosestWalkable();
    }

    public override string ToString() {
        return $"Region {Id} ({(IsObstructed ? "Obstructed" : "Clear")}) at {Center}";
    }
}
