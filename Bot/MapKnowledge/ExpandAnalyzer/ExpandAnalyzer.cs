using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.MapKnowledge;

public class ExpandAnalyzer: INeedUpdating {
    public static ExpandAnalyzer Instance { get; private set; } = new ExpandAnalyzer();
    private const bool DrawEnabled = false;

    private bool _isInitialized = false;
    public static bool IsInitialized => Instance._isInitialized;

    private List<ExpandLocation> _expandLocations;
    public static IReadOnlyCollection<ExpandLocation> ExpandLocations => Instance._expandLocations;

    private List<List<bool>> _tooCloseToResourceGrid;

    private const int TypicalResourceCount = 10;
    private const int ExpandSearchRadius = 5;
    private const int ExpandRadius = 3; // It's 2.5, we put 3 to be safe
    private static readonly float TooCloseToResourceDistance = (float)Math.Sqrt(1*1 + 3*3); // Empirical, 1x3 diagonal

    private ExpandAnalyzer() {}

    public void Reset() {
        Instance = new ExpandAnalyzer();
    }

    public void Update(ResponseObservation observation) {
        if (_isInitialized) {
            return;
        }

        var loadedExpandLocations = ExpandLocationDataStore.Load(Controller.GameInfo.MapName);
        if (loadedExpandLocations != null) {
            Logger.Info("Initializing ExpandAnalyzer from precomputed data for {0}", Controller.GameInfo.MapName);

            _expandLocations = loadedExpandLocations;

            var loadedResourceClusters = FindResourceClusters().ToList();
            foreach (var expandLocation in _expandLocations) {
                var resourceCluster = GetExpandResourceCluster(expandLocation.Position, loadedResourceClusters);
                var blockers = FindExpandBlockers(expandLocation.Position);
                expandLocation.Init(resourceCluster, blockers);
            }

            Logger.Metric("{0} expand locations", _expandLocations.Count);
            Logger.Success("Expand locations loaded from file");

            _isInitialized = true;
            return;
        }

        // For some reason querying for placement doesn't work before a few seconds after the game starts
        if (Controller.Frame < TimeUtils.SecsToFrames(5)) {
            return;
        }

        Logger.Info("Analyzing expand locations, this can take some time...");

        var resourceClusters = FindResourceClusters().ToList();
        Logger.Metric("Found {0} resource clusters", resourceClusters.Count);

        var expandPositions = FindExpandLocations(resourceClusters).ToList();
        Logger.Metric("Found {0} expand locations", expandPositions.Count);

        _isInitialized = expandPositions.Count == resourceClusters.Count;
        if (_isInitialized) {
            _expandLocations = GenerateExpandLocations(expandPositions, resourceClusters);
            ExpandLocationDataStore.Save(Controller.GameInfo.MapName, _expandLocations);
            Logger.Success("Expand analysis done and saved");
        }
        else {
            Logger.Error("Expand analysis failed, the number of resource clusters found does not match the number of expands found");
        }
    }

    // TODO GD Doesn't take into account the building dimensions, but good enough for creep spread since it's 1x1
    public static bool IsNotBlockingExpand(Vector2 position) {
        // We could use Regions here, but I'd rather not because of dependencies
        var closestExpandLocation = Instance._expandLocations
            .Select(expandLocation => expandLocation.Position)
            .MinBy(expandPosition => expandPosition.DistanceTo(position));

        return closestExpandLocation.DistanceTo(position) > ExpandRadius + 1;
    }

    /// <summary>
    /// Gets an expand location of yourself or the enemy
    /// </summary>
    /// <param name="alliance">Yourself or the enemy</param>
    /// <param name="expandType">The expand type</param>
    /// <returns>The requested expand location</returns>
    public static ExpandLocation GetExpand(Alliance alliance, ExpandType expandType) {
        var expands = ExpandLocations.Where(expandLocation => expandLocation.ExpandType == expandType);

        return alliance == Alliance.Enemy
            ? expands.MinBy(expandLocation => expandLocation.Position.DistanceTo(MapAnalyzer.EnemyStartingLocation))!
            : expands.MinBy(expandLocation => expandLocation.Position.DistanceTo(MapAnalyzer.StartingLocation))!;
    }

    private static IEnumerable<List<Unit>> FindResourceClusters() {
        // See note on MineralField450
        var minerals = Controller.GetUnits(UnitsTracker.NeutralUnits, Units.MineralFields.Except(new[] { Units.MineralField450 }).ToHashSet());
        var gasses = Controller.GetUnits(UnitsTracker.NeutralUnits, Units.GasGeysers);
        var resources = minerals.Concat(gasses).ToList();

        return Clustering.DBSCAN(resources, epsilon: 8, minPoints: 4).clusters;
    }

    private IEnumerable<Vector2> FindExpandLocations(List<List<Unit>> resourceClusters) {
        InitTooCloseToResourceGrid(resourceClusters);

        var expandLocations = new List<Vector2>();
        foreach (var resourceCluster in resourceClusters) {
            var centerPosition = Clustering.GetBoundingBoxCenter(resourceCluster).AsWorldGridCenter().ToVector2();
            var searchGrid = MapAnalyzer.BuildSearchGrid(centerPosition, gridRadius: ExpandSearchRadius);

            var goodBuildSpot = searchGrid.FirstOrDefault(IsValidExpandPlacement);
            if (goodBuildSpot != default) {
                expandLocations.Add(goodBuildSpot);
                Program.GraphicalDebugger.AddSphere(goodBuildSpot.ToVector3(), KnowledgeBase.GameGridCellRadius, Colors.Green);
                Program.GraphicalDebugger.AddSphere(centerPosition.ToVector3(), KnowledgeBase.GameGridCellRadius, Colors.Yellow);
            }
        }

        return expandLocations;
    }

    private void InitTooCloseToResourceGrid(List<List<Unit>> resourceClusters) {
        if (_tooCloseToResourceGrid != null) {
            return;
        }

        _tooCloseToResourceGrid = new List<List<bool>>();
        for (var x = 0; x < Controller.GameInfo.StartRaw.MapSize.X; x++) {
            _tooCloseToResourceGrid.Add(new List<bool>(new bool[Controller.GameInfo.StartRaw.MapSize.Y]));
        }

        var cellsTooCloseToResource = resourceClusters.SelectMany(cluster => cluster)
            .SelectMany(FootprintCalculator.GetFootprint)
            .SelectMany(position => MapAnalyzer.BuildSearchRadius(position, TooCloseToResourceDistance))
            .Select(position => position.AsWorldGridCorner());

        foreach (var cell in cellsTooCloseToResource) {
            if (DrawEnabled) {
                Program.GraphicalDebugger.AddGridSquare(cell.AsWorldGridCenter().ToVector3(), Colors.SunbrightOrange);
            }

            _tooCloseToResourceGrid[(int)cell.X][(int)cell.Y] = true;
        }
    }

    private bool IsValidExpandPlacement(Vector2 buildSpot) {
        var footprint = MapAnalyzer.GetBuildingFootprint(buildSpot, Units.Hatchery);
        var footprintIsClear = footprint.All(cell => !_tooCloseToResourceGrid[(int)cell.X][(int)cell.Y]);

        // We'll query just to be sure
        if (footprintIsClear && BuildingTracker.QueryPlacement(Units.Hatchery, buildSpot) != ActionResult.CantBuildTooCloseToResources) {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Generates the ExpandLocations by associating locations and resource cluster, finding blockers and tagging the expand type properly.
    /// </summary>
    /// <param name="expandPositions"></param>
    /// <param name="resourceClusters"></param>
    /// <returns>The ExpandLocations</returns>
    private List<ExpandLocation> GenerateExpandLocations(IReadOnlyList<Vector2> expandPositions, IReadOnlyList<List<Unit>> resourceClusters) {
        // Clusters
        var resourceClustersByExpand = new Dictionary<Vector2, HashSet<Unit>>();
        foreach (var expandPosition in expandPositions) {
            resourceClustersByExpand[expandPosition] = GetExpandResourceCluster(expandPosition, resourceClusters);
        }

        // Blockers
        var blockersByExpand = new Dictionary<Vector2, HashSet<Unit>>();
        foreach (var expandPosition in expandPositions) {
            blockersByExpand[expandPosition] = FindExpandBlockers(expandPosition);
        }

        var expandTypes = new Dictionary<Vector2, ExpandType>();

        // ExpandType based on distance to own base
        var rank = 0;
        foreach (var expandPosition in expandPositions.OrderBy(expandPosition => Pathfinder.FindPath(expandPosition, MapAnalyzer.StartingLocation).Count)) {
            var (expandType, newRank) = CalculateExpandType(resourceClustersByExpand[expandPosition], blockersByExpand[expandPosition], rank);
            expandTypes[expandPosition] = expandType;
            rank = newRank;
        }

        // ExpandType based on distance to enemy base
        rank = 0;
        foreach (var expandPosition in expandPositions.OrderBy(expandPosition => Pathfinder.FindPath(expandPosition, MapAnalyzer.EnemyStartingLocation).Count)) {
            // Already set, skip
            if (expandTypes.ContainsKey(expandPosition) && expandTypes[expandPosition] != ExpandType.Far) {
                continue;
            }

            var (expandType, newRank) = CalculateExpandType(resourceClustersByExpand[expandPosition], blockersByExpand[expandPosition], rank);
            expandTypes[expandPosition] = expandType;
            rank = newRank;
        }

        // ExpandLocations
        var expandLocations = new List<ExpandLocation>();
        foreach (var expandPosition in expandPositions) {
            var resourceCluster = resourceClustersByExpand[expandPosition];
            var blockers = blockersByExpand[expandPosition];
            var expandType = expandTypes[expandPosition];

            var expandLocation = new ExpandLocation(expandPosition, expandType);
            expandLocation.Init(resourceCluster, blockers);

            expandLocations.Add(expandLocation);
        }

        return expandLocations;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="blockers"></param>
    /// <param name="resourceCluster"></param>
    /// <param name="rank"></param>
    /// <returns></returns>
    private static (ExpandType expandType, int newRank) CalculateExpandType(IReadOnlyCollection<Unit> resourceCluster, IEnumerable<Unit> blockers, int rank) {
        if (IsGoldExpand(resourceCluster)) {
            return (ExpandType.Gold, rank);
        }

        if (IsPocketExpand(resourceCluster, blockers)) {
            return (ExpandType.Pocket, rank);
        }

        return (GetExpandTypeByRank(rank), rank + 1);
    }

    /// <summary>
    /// Returns the resource cluster that is the most likely associated with the provided expand position.
    /// </summary>
    /// <param name="expandPosition"></param>
    /// <param name="resourceClusters"></param>
    /// <returns>The resource cluster of that expand position</returns>
    private static HashSet<Unit> GetExpandResourceCluster(Vector2 expandPosition, IEnumerable<List<Unit>> resourceClusters) {
        return resourceClusters.MinBy(cluster => cluster.GetCenter().DistanceTo(expandPosition))!.ToHashSet();
    }

    /// <summary>
    /// Finds all units that need to be cleared to take the expand, typically mineral fields or rocks
    /// </summary>
    /// <param name="expandLocation"></param>
    /// <returns>All units that need to be cleared to take the expand</returns>
    private static HashSet<Unit> FindExpandBlockers(Vector2 expandLocation) {
        var hatcheryRadius = KnowledgeBase.GetBuildingRadius(Units.Hatchery);

        return UnitsTracker.NeutralUnits
            .Where(neutralUnit => neutralUnit.DistanceTo(expandLocation) <= neutralUnit.Radius + hatcheryRadius)
            .ToHashSet();
    }

    /// <summary>
    /// A gold expand has gold minerals and/or purple gas
    /// </summary>
    /// <param name="resourceCluster">The resource cluster associated with the expand</param>
    /// <returns>True if the resource cluster associated with the expand contains rich resources</returns>
    private static bool IsGoldExpand(IEnumerable<Unit> resourceCluster) {
        return resourceCluster.Any(resource => Units.GoldMineralFields.Contains(resource.UnitType) || Units.GoldGasGeysers.Contains(resource.UnitType));
    }

    /// <summary>
    /// A pocket expand has less mineral patches or is blocked
    /// Gold expands also have less mineral patches, but they are not pocket expands
    /// </summary>
    /// <param name="resourceCluster">The resource cluster associated with the expand</param>
    /// <param name="blockers">The units blocking the expand</param>
    /// <returns>True if the expand is a pocket expand</returns>
    private static bool IsPocketExpand(IReadOnlyCollection<Unit> resourceCluster, IEnumerable<Unit> blockers) {
        if (IsGoldExpand(resourceCluster)) {
            return false;
        }

        return blockers.Any() || resourceCluster.Count < TypicalResourceCount;
    }

    private static ExpandType GetExpandTypeByRank(int rank) {
        return rank switch
        {
            0 => ExpandType.Main,
            1 => ExpandType.Natural,
            2 => ExpandType.Third,
            3 => ExpandType.Fourth,
            4 => ExpandType.Fifth,
            _ => ExpandType.Far,
        };
    }
}
