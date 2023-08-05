using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Sajuuk.Algorithms;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis.ExpandAnalysis;
using SC2APIProtocol;

namespace Sajuuk.MapAnalysis.RegionAnalysis;

public class Region : IRegion {
    private ITerrainTracker _terrainTracker;
    private IClustering _clustering;
    private IPathfinder _pathfinder;

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
        IPathfinder pathfinder
    ) {
        _terrainTracker = terrainTracker;
        _clustering = clustering;
        _pathfinder = pathfinder;
    }

    public void SetDependencies(
        ITerrainTracker terrainTracker,
        IClustering clustering,
        IPathfinder pathfinder
    ) {
        _terrainTracker = terrainTracker;
        _clustering = clustering;
        _pathfinder = pathfinder;
    }

    public IEnumerable<IRegion> GetReachableNeighbors() {
        return Neighbors
            .Where(neighbor => !neighbor.Region.IsObstructed)
            .Where(neighbor => {
                // A neighbor is only reachable if the path to it goes through the neighbor's frontier
                var origin = _terrainTracker.GetClosestWalkable(Center, allowedCells: Cells);
                var destination = _terrainTracker.GetClosestWalkable(neighbor.Region.Center, allowedCells: neighbor.Region.Cells);
                var path = _pathfinder.FindPath(origin, destination);

                // That's a bit awkward, but diagonals can cross without touching
                // A single frontier contains diagonals
                // The two frontiers form a line without diagonals
                // An alternative would be to check if the path only goes through this region or the neighbors, but I don't know if it's always true.
                //
                // The real solution is to find a path with only the cells in either regions, but that's hard to cache!
                var frontierCells = neighbor.Region.Neighbors
                    .First(mirrorNeighbor => mirrorNeighbor.Region == this)
                    .Frontier
                    .Concat(neighbor.Frontier)
                    .ToHashSet();

                return path.Any(frontierCells.Contains);
            })
            .Select(neighbor => neighbor.Region);
    }

    public void UpdateObstruction() {
        if (IsObstructed) {
            IsObstructed = IsRegionObstructed();
        }
    }

    protected bool IsRegionObstructed() {
        // I **think** only ramps can be obstructed
        if (Type != RegionType.Ramp) {
            return false;
        }

        if (!Cells.Any(cell => _terrainTracker.IsWalkable(cell))) {
            return true;
        }

        var frontier = Neighbors.SelectMany(neighbor => neighbor.Frontier).ToList();
        var clusteringResult = _clustering.DBSCAN(frontier, epsilon: (float)Math.Sqrt(2), minPoints: 1);
        if (clusteringResult.clusters.Count != 2) {
            Logger.Error("This ramp has {0} frontiers instead of the expected 2", clusteringResult.clusters.Count);
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
