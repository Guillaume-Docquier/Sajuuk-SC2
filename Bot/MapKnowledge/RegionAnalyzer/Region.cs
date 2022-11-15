using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Bot.ExtensionMethods;
using Bot.Utils;

namespace Bot.MapKnowledge;

public class Region {
    public HashSet<Vector2> Cells { get; }
    public Vector2 Center { get; }
    public RegionType Type { get; }
    public bool IsObstructed { get; private set; }

    [JsonIgnore]
    public ExpandLocation ExpandLocation { get; private set; }

    [JsonIgnore]
    public HashSet<NeighboringRegion> Neighbors { get; private set; }

    [JsonConstructor]
    public Region(HashSet<Vector2> cells, Vector2 center, RegionType type, bool isObstructed) {
        Cells = cells;
        Center = center;
        Type = type;
        IsObstructed = isObstructed;
    }

    public Region(IEnumerable<Vector2> cells, RegionType type) {
        Cells = cells.ToHashSet();

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

    public void Init(bool computeObstruction) {
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
    }

    // TODO GD Some ramps touch 3 regions
    // The rocks footprints makes it hard for Pathfinder to find proper paths
    private bool IsRegionObstructed() {
        // I **think** only ramps can be obstructed
        if (Type != RegionType.Ramp) {
            return false;
        }

        var frontier = Neighbors.SelectMany(neighbor => neighbor.Frontier).ToList();
        var clusteringResult = Clustering.DBSCAN(frontier, epsilon: (float)Math.Sqrt(2), minPoints: 1);
        if (clusteringResult.clusters.Count != 2) {
            Logger.Error("This ramp has {0} frontiers instead of the expected 2", clusteringResult.clusters.Count);
            return false;
        }

        var pathThrough = Pathfinder.FindPath(
            clusteringResult.clusters[0].First(cell => MapAnalyzer.IsWalkable(cell)),
            clusteringResult.clusters[1].First(cell => MapAnalyzer.IsWalkable(cell))
        );

        if (pathThrough == null) {
            return true;
        }

        // Obstructed if the path between the neighbors does not go through this region
        // Let's hope no regions have two direct paths between each other
        var portionPassingThrough = (float)pathThrough.Count(cell => Cells.Contains(cell)) / pathThrough.Count;
        return portionPassingThrough <= 0.5f;
    }
}
