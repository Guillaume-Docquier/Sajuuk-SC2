using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.MapKnowledge;

// TODO GD Precompute (runs in ~5s)
// TODO GD Find choke points
// Graph theory: https://en.wikipedia.org/wiki/Bridge_(graph_theory)#Tarjan's_bridge-finding_algorithm
// Voronoi decomposition: https://citeseerx.ist.psu.edu/viewdoc/download;jsessionid=F7CE5598E6DBFA934A8E159433181AF6?doi=10.1.1.728.5136&rep=rep1&type=pdf
public class RegionAnalyzer: INeedUpdating {
    private const bool DrawEnabled = false;

    private const int MinRampSize = 5;
    private const int RegionMinPoints = 6;
    private const float RegionZMultiplier = 8;
    private static readonly float DiagonalDistance = (float)Math.Sqrt(2);

    private class MapCell: IHavePosition {
        public MapCell(float x, float y) {
            Position = new Vector3(x, y, 0).AsWorldGridCenter().WithWorldHeight();
        }

        public Vector3 Position { get; set; }
    }

    public static readonly RegionAnalyzer Instance = new RegionAnalyzer();

    public static readonly List<HashSet<Vector3>> Regions = new List<HashSet<Vector3>>();
    public static readonly List<HashSet<Vector3>> Ramps = new List<HashSet<Vector3>>();
    public static readonly List<Vector3> Noise = new List<Vector3>();

    private static bool _isInitialized = false;

    private readonly List<Color> _regionColors = new List<Color>
    {
        Colors.MulberryRed,
        Colors.MediumTurquoise,
        Colors.SunbrightOrange,
        Colors.PeachPink,
        Colors.Purple,
        Colors.LimeGreen,
        Colors.BurlywoodBeige,
        Colors.LightRed,
    };

    /// <summary>
    /// <para>Analyzes the map to find ramps and do region decomposition using clustering techniques.</para>
    /// <para>There should be at least 1 region per expand location and ramps always separate two regions.</para>
    /// </summary>
    public void Update(ResponseObservation observation) {
        if (!ExpandAnalyzer.IsInitialized) {
            return;
        }

        if (_isInitialized) {
            if (Program.DebugEnabled && DrawEnabled) {
                DrawRegions();
                DrawRamps();
                DrawNoise();
            }

            return;
        }

        var map = GenerateMap();

        Logger.Info("Starting region analysis on {0} cells ({1}x{2})", map.Count, MapAnalyzer.MaxX, MapAnalyzer.MaxY);

        var regionsNoise = InitRegions(map);
        var rampsNoise = InitRamps(regionsNoise);
        Noise.AddRange(rampsNoise.Select(mapCell => mapCell.Position));

        Logger.Info("Region analysis done");
        Logger.Info("{0} regions, {1} ramps and {2} unclassified cells", Regions.Count, Ramps.Count, Noise.Count);

        _isInitialized = true;
    }

    /// <summary>
    /// <para>Draws a square on each region's cells.</para>
    /// <para>Each region gets a different color using the color pool.</para>
    /// <para>Each cell also gets a text 'EX', where E stands for 'Expand' and X is the region index.</para>
    /// </summary>
    private void DrawRegions() {
        var regionIndex = 0;
        foreach (var region in Regions) {
            foreach (var position in region) {
                GraphicalDebugger.AddText($"E{regionIndex}", size: 12, worldPos: position.WithWorldHeight().ToPoint(), color: _regionColors[regionIndex % _regionColors.Count]);
                GraphicalDebugger.AddGridSquare(position.WithWorldHeight(), _regionColors[regionIndex % _regionColors.Count]);
            }

            regionIndex++;
        }
    }

    /// <summary>
    /// <para>Draws a sphere on each ramp's cells.</para>
    /// <para>Each ramp gets a different color using the color pool.</para>
    /// <para>Each cell also gets a text 'RX', where E stands for 'Ramp' and X is the ramp index.</para>
    /// </summary>
    private void DrawRamps() {
        var rampIndex = 0;
        foreach (var ramp in Ramps) {
            foreach (var position in ramp) {
                GraphicalDebugger.AddText($"R{rampIndex}", size: 12, worldPos: position.WithWorldHeight().ToPoint(), color: _regionColors[rampIndex % _regionColors.Count]);
                GraphicalDebugger.AddGridSphere(position.WithWorldHeight(), _regionColors[rampIndex % _regionColors.Count]);
            }

            rampIndex++;
        }
    }

    /// <summary>
    /// <para>Draws a red square on each noise cell.</para>
    /// <para>A noise cell is a cell that isn't part of a region or ramp.</para>
    /// <para>Each cell also gets a text '?'.</para>
    /// </summary>
    private static void DrawNoise() {
        foreach (var position in Noise) {
            GraphicalDebugger.AddText("?", size: 12, worldPos: position.WithWorldHeight().ToPoint(), color: Colors.Red);
            GraphicalDebugger.AddGridSphere(position.WithWorldHeight(), Colors.Red);
        }
    }

    /// <summary>
    /// Generates a list of MapCell representing each playable tile in the map.
    /// </summary>
    private static List<MapCell> GenerateMap() {
        var map = new List<MapCell>();
        for (var x = 0; x < MapAnalyzer.MaxX; x++) {
            for (var y = 0; y < MapAnalyzer.MaxY; y++) {
                var mapCell = new MapCell(x, y);
                if (MapAnalyzer.IsWalkable(mapCell.Position, includeObstacles: false)) {
                    map.Add(mapCell);
                }
            }
        }

        return map;
    }

    /// <summary>
    /// <para>Computes the regions by using clustering and highly penalizing height differences.</para>
    /// <para>This penalty will allow us to isolate ramps by turning them into noise, which we can cluster properly afterwards.</para>
    /// <para>We will then try to break down each region into sub regions, because not all regions are separated by ramps.</para>
    /// </summary>
    /// <returns>
    /// Cells that are not part of any region.
    /// </returns>
    private static List<MapCell> InitRegions(List<MapCell> cells) {
        cells.ForEach(mapCell => {
            // Highly penalize height differences
            var trickPosition = mapCell.Position;
            trickPosition.Z *= RegionZMultiplier;
            mapCell.Position = trickPosition;
        });

        var regionClusteringResult = Clustering.DBSCAN(cells, epsilon: DiagonalDistance + 0.04f, minPoints: RegionMinPoints);
        foreach (var region in regionClusteringResult.clusters) {
            var subregions = BreakDownIntoSubregions(region.Select(cell => cell.Position.WithoutZ()).ToList());
            foreach (var subregion in subregions) {
                Regions.Add(subregion.ToHashSet());
            }
        }

        return regionClusteringResult.noise;
    }

    /// <summary>
    /// <para>Break down a given region into subregions based on expand locations.</para>
    /// <para>If a regions contains a single expand location or none, it is not broken down.</para>
    /// <para>We will calculate the distance from each position in the region to each expand in that region.</para>
    /// <para>Using dynamic programming, we will be able to do a single pass over the region for each expand.</para>
    /// <para>We'll resolve the closest expand with an extra pass on the region.</para>
    /// </summary>
    /// <returns>
    /// A list of subregions.
    /// </returns>
    private static List<List<Vector3>> BreakDownIntoSubregions(List<Vector3> region) {
        var regionMap = region.ToHashSet();
        var expandsInRegion = ExpandAnalyzer.ExpandLocations.Select(expand => expand.WithoutZ()).Where(expand => regionMap.Contains(expand)).ToList();

        // Single or no expand, no subregion
        if (expandsInRegion.Count <= 1) {
            return new List<List<Vector3>> { region };
        }

        // Calculate the distance from each position to each expand in the region

        // distanceToExpands[position][expand] = distance;
        var distanceToExpands = region.ToDictionary(positionInRegion => positionInRegion, _ => expandsInRegion.ToDictionary(expand => expand, _ => float.MaxValue));
        foreach (var expand in expandsInRegion) {
            var positionsToExplore = new Queue<(Vector3, float)>();
            positionsToExplore.Enqueue((expand, 0));
            distanceToExpands[expand][expand] = 0;

            while (positionsToExplore.Any()) {
                var (position, distance) = positionsToExplore.Dequeue();
                foreach (var neighbor in position.GetNeighbors().Where(regionMap.Contains)) {
                    var distanceToNeighbor = distance + neighbor.HorizontalDistanceTo(position);
                    if (distanceToNeighbor < distanceToExpands[neighbor][expand]) {
                        positionsToExplore.Enqueue((neighbor, distanceToNeighbor));
                        distanceToExpands[neighbor][expand] = distanceToNeighbor;
                    }
                }
            }
        }

        // Find the closest expand to each position given the distances we just computed
        // Each expand will have its own region
        var subregions = expandsInRegion.ToDictionary(expand => expand, _ => new List<Vector3>());
        foreach (var (position, distances) in distanceToExpands) {
            var closestExpand = distances.MinBy(distanceToExpand => distanceToExpand.Value).Key;
            subregions[closestExpand].Add(position);
        }

        return subregions.Values.ToList();
    }

    /// <summary>
    /// Identify ramps given cells that are not part of any regions using clustering.
    /// </summary>
    /// <returns>
    /// Cells that are not part of any ramp.
    /// </returns>
    private static IEnumerable<MapCell> InitRamps(List<MapCell> cells) {
        cells.ForEach(mapCell => mapCell.Position = mapCell.Position.WithWorldHeight()); // Reset the Z

        var rampClusteringResult = Clustering.DBSCAN(cells, epsilon: DiagonalDistance * 2, minPoints: MinRampSize);
        foreach (var ramp in rampClusteringResult.clusters) {
            Ramps.Add(ramp.Select(cell => cell.Position.WithoutZ()).ToHashSet());
        }

        return rampClusteringResult.noise;
    }
}
