using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.Wrapper;

namespace Bot.MapKnowledge;

public static class MapAnalyzer {
    public static bool IsInitialized = false;

    public static List<List<Unit>> ResourceClusters;
    public static List<Vector3> ExpandLocations;

    private const float ExpandSearchRadius = 5;

    // The controller must be initialized first
    public static void Init() {
        if (IsInitialized) {
            return;
        }

        Logger.Info("Initializing MapAnalyzer");

        ResourceClusters = FindResourceClusters().ToList();
        Logger.Info("Found {0} resource clusters", ResourceClusters.Count);

        ExpandLocations = FindExpandLocations().ToList();
        ExpandLocations.Add(Controller.StartingTownHall.Position); // Not found because already built
        Logger.Info("Found {0} expand locations", ExpandLocations.Count);

        IsInitialized = ExpandLocations.Count == ResourceClusters.Count;

        Logger.Info("{0}", IsInitialized ? "Success!" : "Failed...");
    }

    private static IEnumerable<List<Unit>> FindResourceClusters() {
        var minerals = Controller.GetUnits(Controller.NeutralUnits, Units.MineralFields);
        var gasses = Controller.GetUnits(Controller.NeutralUnits, Units.GasGeysers);
        var resources = minerals.Concat(gasses).ToList();

        return Clustering.DBSCAN(resources, epsilon: 7, minPoints: 4);
    }

    private static IEnumerable<Vector3> FindExpandLocations() {
        var expandLocations = new List<Vector3>();

        foreach (var resourceCluster in ResourceClusters) {
            var centerPosition = Clustering.GetBoundingBoxCenter(resourceCluster).AsWorldGridCenter();
            var searchGrid = BuildSearchGrid(centerPosition, gridRadius: ExpandSearchRadius);

            var goodBuildSpot = searchGrid.FirstOrDefault(buildSpot => Controller.CanPlace(Units.Hatchery, buildSpot));
            if (goodBuildSpot != default) {
                expandLocations.Add(goodBuildSpot);
                GraphicalDebugger.AddSphere(goodBuildSpot, KnowledgeBase.GameGridCellRadius, Colors.Green);
                GraphicalDebugger.AddSphere(centerPosition, KnowledgeBase.GameGridCellRadius, Colors.Yellow);
            }
        }

        return expandLocations;
    }

    public static IEnumerable<Vector3> BuildSearchGrid(Vector3 centerPosition, float gridRadius, float stepSize = KnowledgeBase.GameGridCellWidth) {
        var buildSpots = new List<Vector3>();
        for (var x = centerPosition.X - gridRadius; x <= centerPosition.X + gridRadius; x += stepSize) {
            for (var y = centerPosition.Y - gridRadius; y <= centerPosition.Y + gridRadius; y += stepSize) {
                if (!Pathfinder.IsInitialized || Pathfinder.IsInBounds(x, y)) {
                    buildSpots.Add(new Vector3(x, y, centerPosition.Z));
                }
            }
        }

        return buildSpots.OrderBy(position => Vector3.Distance(centerPosition, position));
    }
}
