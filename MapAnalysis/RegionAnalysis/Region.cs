using System.Numerics;
using System.Text.Json.Serialization;
using SC2APIProtocol;

namespace MapAnalysis.RegionAnalysis;

public class Region : IRegion {
    private ITerrainTracker _terrainTracker;
    private IClustering _clustering;
    private IPathfinder _pathfinder;
    private IUnitsTracker _unitsTracker;

    [JsonInclude] public int Id { get; set; }
    [JsonInclude] public Color Color { get; set; }
    [JsonInclude] public Vector2 Center { get; set; }
    [JsonInclude] public HashSet<Vector2> Cells { get; protected set; }
    [JsonInclude] public float ApproximatedRadius { get; set; }
    [JsonInclude] public RegionType Type { get; set; }
    [JsonInclude] public bool IsObstructed { get; set; }

    [JsonInclude] public ExpandLocation ConcreteExpandLocation { get; set; }
    [JsonIgnore] public IExpandLocation ExpandLocation => ConcreteExpandLocation;

    [JsonInclude] public HashSet<NeighboringRegion> ConcreteNeighbors { get; set; }
    [JsonIgnore] public IEnumerable<INeighboringRegion> Neighbors => ConcreteNeighbors;

    [JsonConstructor]
    [Obsolete("Do not use this parameterless JsonConstructor", error: true)]
    public Region() {}

    public Region(
        ITerrainTracker terrainTracker,
        IClustering clustering,
        IPathfinder pathfinder,
        IUnitsTracker unitsTracker
    ) {
        _terrainTracker = terrainTracker;
        _clustering = clustering;
        _pathfinder = pathfinder;
        _unitsTracker = unitsTracker;
    }

    public void SetDependencies(
        ITerrainTracker terrainTracker,
        IClustering clustering,
        IPathfinder pathfinder,
        IUnitsTracker unitsTracker
    ) {
        _terrainTracker = terrainTracker;
        _clustering = clustering;
        _pathfinder = pathfinder;
        _unitsTracker = unitsTracker;
    }

    public IEnumerable<IRegion> GetReachableNeighbors() {
        return Neighbors
            .Where(neighbor => !neighbor.Region.IsObstructed)
            .Select(neighbor => neighbor.Region);
    }

    // TODO GD Regions should track units in their region and update obstructions themselves when these units die
    public void UpdateObstruction() {
        IsObstructed = IsRegionObstructed();
    }

    protected bool IsRegionObstructed() {
        if (Type == RegionType.Expand) {
            // Expands are never obstructed
            return false;
        }

        if (Cells.All(cell => !_terrainTracker.IsWalkable(cell))) {
            return true;
        }

        var obstaclesInRegion = _unitsTracker
            .GetUnits(_unitsTracker.NeutralUnits, Units.Obstacles.Concat(Units.MineralFields).ToHashSet())
            .Where(obstacle => Cells.Contains(obstacle.Position.ToVector2().AsWorldGridCenter()));

        if (!obstaclesInRegion.Any()) {
            return false;
        }

        var frontier = Neighbors.SelectMany(neighbor => neighbor.Frontier).ToList();
        var clusteringResult = _clustering.DBSCAN(frontier, epsilon: (float)Math.Sqrt(2), minPoints: 1);
        if (clusteringResult.clusters.Count != 2) {
            Logger.Warning($"Region {Id} has {clusteringResult.clusters.Count} frontiers instead of 2, we cannot determine if it is obstructed.");
            return false;
        }

        var pathThrough = _pathfinder.FindPath(
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

    private Vector2 GetWalkableCellNearFrontier(IReadOnlyCollection<Vector2> frontier) {
        var walkableCell = frontier.FirstOrDefault(cell => _terrainTracker.IsWalkable(cell));

        return walkableCell != default
            ? walkableCell
            : _terrainTracker.GetClosestWalkable(frontier.First());
    }

    public override string ToString() {
        return $"Region {Id} ({(IsObstructed ? "Obstructed" : "Clear")}) at {Center}";
    }
}
