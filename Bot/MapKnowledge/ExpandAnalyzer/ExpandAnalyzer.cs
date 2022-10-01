using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.MapKnowledge;

public class ExpandAnalyzer: INeedUpdating {
    public static readonly ExpandAnalyzer Instance = new ExpandAnalyzer();
    public static bool IsInitialized { get; private set; }

    public static List<List<Unit>> ResourceClusters { get; private set; }
    public static List<Vector3> ExpandLocations { get; private set; }

    private const int ExpandSearchRadius = 5;
    private const int ExpandRadius = 3; // It's 2.5, we put 3 to be safe
    private static readonly float TooCloseToResourceDistance = (float)Math.Sqrt(1*1 + 3*3);

    private static List<List<bool>> _tooCloseToResourceGrid;

    private ExpandAnalyzer() {}

    public void Update(ResponseObservation observation) {
        if (IsInitialized) {
            foreach (var expandLocation in ExpandLocations) {
                Program.GraphicalDebugger.AddSphere(expandLocation, KnowledgeBase.GameGridCellRadius, Colors.Green);
            }

            return;
        }

        var expandsData = ExpandDataStore.Load(Controller.GameInfo.MapName);
        if (expandsData != null) {
            Logger.Info("Initializing ExpandAnalyzer from precomputed data for {0}", Controller.GameInfo.MapName);

            // This isn't expensive, but I guess we could still save it?
            ResourceClusters = FindResourceClusters().ToList();
            Logger.Info("Found {0} resource clusters", ResourceClusters.Count);

            ExpandLocations = expandsData;
            Logger.Info("Found {0} expand locations", ExpandLocations.Count);

            IsInitialized = true;
            return;
        }

        // For some reason querying for placement doesn't work before a few seconds after the game starts
        if (Controller.Frame < Controller.SecsToFrames(5)) {
            return;
        }

        Logger.Info("Analyzing expand locations, this can take some time...");

        ResourceClusters = FindResourceClusters().ToList();
        Logger.Info("Found {0} resource clusters", ResourceClusters.Count);

        ExpandLocations = FindExpandLocations().ToList();
        Logger.Info("Found {0} expand locations", ExpandLocations.Count);

        IsInitialized = ExpandLocations.Count == ResourceClusters.Count;
        if (IsInitialized) {
            ExpandDataStore.Save(Controller.GameInfo.MapName, ExpandLocations);
            Logger.Info("Expand analysis done and saved");
        }
        else {
            Logger.Info("Expand analysis failed, the number of resource clusters found does not match the number of expands found");
        }
    }

    // TODO GD Doesn't take into account the building dimensions, but good enough for creep spread since it's 1x1
    public static bool IsNotBlockingExpand(Vector3 position) {
        return ExpandLocations.MinBy(expandLocation => expandLocation.HorizontalDistanceTo(position)).HorizontalDistanceTo(position) > ExpandRadius + 1;
    }

    private static IEnumerable<List<Unit>> FindResourceClusters() {
        // See note on MineralField450
        var minerals = Controller.GetUnits(UnitsTracker.NeutralUnits, Units.MineralFields.Except(new[] { Units.MineralField450 }).ToHashSet());
        var gasses = Controller.GetUnits(UnitsTracker.NeutralUnits, Units.GasGeysers);
        var resources = minerals.Concat(gasses).ToList();

        return Clustering.DBSCAN(resources, epsilon: 7, minPoints: 4).clusters;
    }

    private static IEnumerable<Vector3> FindExpandLocations() {
        InitTooCloseToResourceGrid();

        var expandLocations = new List<Vector3>();
        foreach (var resourceCluster in ResourceClusters) {
            var centerPosition = Clustering.GetBoundingBoxCenter(resourceCluster).AsWorldGridCenter();
            var searchGrid = MapAnalyzer.BuildSearchGrid(centerPosition, gridRadius: ExpandSearchRadius);

            var goodBuildSpot = searchGrid.FirstOrDefault(IsValidExpandPlacement);
            if (goodBuildSpot != default) {
                expandLocations.Add(goodBuildSpot);
                Program.GraphicalDebugger.AddSphere(goodBuildSpot, KnowledgeBase.GameGridCellRadius, Colors.Green);
                Program.GraphicalDebugger.AddSphere(centerPosition, KnowledgeBase.GameGridCellRadius, Colors.Yellow);
            }
        }

        return expandLocations;
    }

    private static void InitTooCloseToResourceGrid() {
        if (_tooCloseToResourceGrid != null) {
            return;
        }

        _tooCloseToResourceGrid = new List<List<bool>>();
        for (var x = 0; x < Controller.GameInfo.StartRaw.MapSize.X; x++) {
            _tooCloseToResourceGrid.Add(new List<bool>(new bool[Controller.GameInfo.StartRaw.MapSize.Y]));
        }

        var cellsTooCloseToResource = ResourceClusters.SelectMany(cluster => cluster)
            .SelectMany(MapAnalyzer.GetObstacleFootprint)
            .SelectMany(position => MapAnalyzer.BuildSearchRadius(position, TooCloseToResourceDistance))
            .Select(position => position.AsWorldGridCorner());

        foreach (var cell in cellsTooCloseToResource) {
            Program.GraphicalDebugger.AddGridSquare(cell.AsWorldGridCenter(), Colors.SunbrightOrange);
            _tooCloseToResourceGrid[(int)cell.X][(int)cell.Y] = true;
        }
    }

    private static bool IsValidExpandPlacement(Vector3 buildSpot) {
        var footprint = MapAnalyzer.BuildSearchGrid(buildSpot, Buildings.Dimensions[Units.Hatchery].Height / 2);
        var footprintIsClear = footprint.All(cell => !_tooCloseToResourceGrid[(int)cell.X][(int)cell.Y]);

        // We'll query just to be sure
        if (footprintIsClear && BuildingTracker.QueryPlacement(Units.Hatchery, buildSpot) != ActionResult.CantBuildTooCloseToResources) {
            return true;
        }

        return false;
    }
}
