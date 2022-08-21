using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.MapKnowledge;

public class RegionAnalyzer: INeedUpdating {
    private class MapCell: IHavePosition {
        public MapCell(int x, int y) {
            var position = new Vector3(x, y, 0).AsWorldGridCenter().WithWorldHeight();

            // We multiply Z by 10 to increase the distance with cells not on the same height during clustering
            // It will allow us to isolate ramps
            position.Z *= 10;

            Position = position;
        }

        public Vector3 Position { get; private set; }
    }

    public static readonly RegionAnalyzer Instance = new RegionAnalyzer();

    public static readonly List<HashSet<Vector3>> Regions = new List<HashSet<Vector3>>();

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
            var colorIndex = 0;
            foreach (var region in Regions) {
                if (UnitsTracker.OwnedUnits.Any(unit => region.Contains(unit.Position.AsWorldGridCenter().WithoutZ()))) {
                    foreach (var position in region) {
                        GraphicalDebugger.AddGridSquare(position.WithWorldHeight(), _regionColors[colorIndex]);
                    }

                    colorIndex = (colorIndex + 1) % _regionColors.Count;
                }
            }

            return;
        }

        // Generate the map of walkable cells
        var map = new List<MapCell>();
        for (var x = 0; x < MapAnalyzer.MaxX; x++) {
            for (var y = 0; y < MapAnalyzer.MaxY; y++) {
                var mapCell = new MapCell(x, y);
                if (MapAnalyzer.IsWalkable(mapCell.Position, includeObstacles: false)) {
                    map.Add(mapCell);
                }
            }
        }

        Logger.Info("Starting region analysis on {0} cells ({1}x{2})", map.Count, MapAnalyzer.MaxX, MapAnalyzer.MaxY);

        // Cluster
        const float diagonalDistance = 1.41f;
        var regions = Clustering.DBSCAN(map, epsilon: diagonalDistance + 0.04f, minPoints: 3).ToList();

        // Save
        foreach (var region in regions) {
            Regions.Add(region.Select(cell => cell.Position.WithoutZ()).ToHashSet());
        }

        // TODO GD Use noise to find ramps
        // TODO GD Break down big regions based on distance to expand

        Logger.Info("Region analysis done");
        _isInitialized = true;
    }
}
