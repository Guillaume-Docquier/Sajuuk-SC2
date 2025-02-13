using System.Numerics;
using System.Text.Json.Serialization;
using Algorithms;
using Algorithms.ExtensionMethods;
using MapAnalysis.ExpandAnalysis;
using SC2APIProtocol;
using SC2Client.Debugging.GraphicalDebugging;
using SC2Client.ExtensionMethods;
using SC2Client.GameData;
using SC2Client.Logging;
using SC2Client.Services;
using SC2Client.State;
using SC2Client.Trackers;

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
    [JsonInclude] public bool IsObstructed { get; set; }
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

    public IEnumerable<IRegion> GetReachableNeighbors() {
        return Neighbors
            .Where(neighbor => !neighbor.Region.IsObstructed)
            .Select(neighbor => neighbor.Region);
    }

    // TODO GD Do I really need this? Maybe a builder pattern would help
    public void FinalizeCreation(int id, IEnumerable<Region> allRegions, ITerrainTracker terrainTracker, IUnitsTracker unitsTracker, IPathfinder<Vector2> pathfinder, ILogger logger) {
        Id = id;

        Neighbors = ComputeNeighboringRegions(Cells, allRegions.Where(region => region != this).ToHashSet())
            .OrderBy(neighbor => neighbor.Region.Id)
            .ToHashSet();

        // TODO GD The signature is ugly but only needed for this, which I want to get rid of.
        IsObstructed = IsRegionObstructed(terrainTracker, unitsTracker, pathfinder, logger);
        Color = GetDifferentColorFromNeighbors();
    }

    [Obsolete("Will be removed when obstruction is reworked")]
    private bool IsRegionObstructed(ITerrainTracker terrainTracker, IUnitsTracker unitsTracker, IPathfinder<Vector2> pathfinder, ILogger logger) {
        if (Type == RegionType.Expand) {
            // Expands are never obstructed
            return false;
        }

        if (Cells.All(cell => !terrainTracker.IsWalkable(cell))) {
            return true;
        }

        var obstaclesInRegion = UnitQueries
            .GetUnits(unitsTracker.NeutralUnits, UnitTypeId.Obstacles.Concat(UnitTypeId.MineralFields).ToHashSet())
            .Where(obstacle => Cells.Contains(obstacle.Position.ToVector2().AsWorldGridCenter()));

        if (!obstaclesInRegion.Any()) {
            return false;
        }

        var frontier = Neighbors.SelectMany(neighbor => neighbor.Frontier).ToList();
        var clusteringResult = Clustering.DBSCAN(frontier, epsilon: (float)Math.Sqrt(2), minPoints: 1);
        if (clusteringResult.clusters.Count != 2) {
            logger.Warning($"Region {Id} has {clusteringResult.clusters.Count} frontiers instead of 2, we cannot determine if it is obstructed.");
            return false;
        }

        var pathThrough = pathfinder.FindPath(
            GetWalkableCellNearFrontier(clusteringResult.clusters[0], terrainTracker),
            GetWalkableCellNearFrontier(clusteringResult.clusters[1], terrainTracker)
        );

        if (pathThrough == null) {
            return true;
        }

        // Obstructed if the path between the neighbors does not go through this region
        // Let's hope no regions have two direct paths between each other
        var portionPassingThrough = (float)pathThrough.Count(cell => Cells.Contains(cell)) / pathThrough.Count;
        return portionPassingThrough <= 0.5f;
    }

    [Obsolete("Will be removed when obstruction is reworked")]
    private Vector2 GetWalkableCellNearFrontier(IReadOnlyCollection<Vector2> frontier, ITerrainTracker terrainTracker) {
        var walkableCell = frontier.FirstOrDefault(cell => terrainTracker.IsWalkable(cell));

        return walkableCell != default
            ? walkableCell
            : terrainTracker.GetClosestWalkable(frontier.First());
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
