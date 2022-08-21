using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.MapKnowledge;

public class RegionAnalyzer: INeedUpdating {
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

    public void Update(ResponseObservation observation) {
        if (_isInitialized) {
            DrawRegions();
            DrawRamps();
            DrawNoise();

            return;
        }

        var map = GenerateMap();

        Logger.Info("Starting region analysis on {0} cells ({1}x{2})", map.Count, MapAnalyzer.MaxX, MapAnalyzer.MaxY);

        var regionsNoise = ComputeRegions(map);
        var rampsNoise = ComputeRamps(regionsNoise);
        Noise.AddRange(rampsNoise.Select(mapCell => mapCell.Position));

        Logger.Info("Region analysis done");
        Logger.Info("{0} regions, {1} ramps and {2} unclassified cells", Regions.Count, Ramps.Count, Noise.Count);

        _isInitialized = true;
    }

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

    private static void DrawNoise() {
        foreach (var position in Noise) {
            GraphicalDebugger.AddText("?", size: 12, worldPos: position.WithWorldHeight().ToPoint(), color: Colors.Red);
            GraphicalDebugger.AddGridSphere(position.WithWorldHeight(), Colors.Red);
        }
    }

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

    // TODO GD Break down big regions based on distance to expand
    private static List<MapCell> ComputeRegions(List<MapCell> cells) {
        cells.ForEach(mapCell => {
            // We multiply Z to increase the distance with cells not on the same height during clustering
            // It will allow us to isolate ramps by turning them into noise, which we can cluster properly afterwards
            var trickPosition = mapCell.Position;
            trickPosition.Z *= RegionZMultiplier;
            mapCell.Position = trickPosition;
        });

        var regionClusteringResult = Clustering.DBSCAN(cells, epsilon: DiagonalDistance + 0.04f, minPoints: RegionMinPoints);
        foreach (var region in regionClusteringResult.clusters) {
            Regions.Add(region.Select(cell => cell.Position.WithoutZ()).ToHashSet());
        }

        return regionClusteringResult.noise;
    }

    private static IEnumerable<MapCell> ComputeRamps(List<MapCell> cells) {
        cells.ForEach(mapCell => mapCell.Position = mapCell.Position.WithWorldHeight()); // Reset the Z

        var rampClusteringResult = Clustering.DBSCAN(cells, epsilon: DiagonalDistance * 2, minPoints: MinRampSize);
        foreach (var ramp in rampClusteringResult.clusters) {
            Ramps.Add(ramp.Select(cell => cell.Position.WithoutZ()).ToHashSet());
        }

        return rampClusteringResult.noise;
    }
}
