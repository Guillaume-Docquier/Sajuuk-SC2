using System.Numerics;
using Algorithms;
using Algorithms.ExtensionMethods;
using SC2APIProtocol;
using SC2Client;
using SC2Client.Debugging.GraphicalDebugging;
using SC2Client.ExtensionMethods;
using SC2Client.GameData;
using SC2Client.Services;
using SC2Client.State;
using SC2Client.Trackers;

namespace MapAnalysis.ExpandAnalysis;

public class ExpandAnalyzer : IExpandAnalyzer {
    private readonly KnowledgeBase _knowledgeBase;
    private readonly IFrameClock _frameClock;
    private readonly ILogger _logger;
    private readonly ISc2Client _sc2Client;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IResourceFinder _resourceFinder;
    private readonly IPathfinder<Vector2> _pathfinder;
    private readonly FootprintCalculator _footprintCalculator;

    private const bool DrawEnabled = false;

    /// <summary>
    /// The number of resources that we typically expect around an expand location.
    /// 8 mineral fields and 2 vespene geysers.
    /// </summary>
    private const int TypicalResourceCount = 10;

    /// <summary>
    /// The radius in which to search for an optimal expand location.
    /// If we have to look farther than that, we're probably not at an expand location.
    /// </summary>
    private const int ExpandSearchRadius = 5;

    /// <summary>
    /// The minimum distance a townhall has to be from a resource in order to be able to build it.
    /// </summary>
    private static readonly float TooCloseToResourceDistance = (float)Math.Sqrt(1*1 + 3*3); // Empirical, 1x3 diagonal

    /// <summary>
    /// The cells for each resource clusters where a townhall can't be built because it would be too close to a resource.
    /// </summary>
    private List<List<bool>>? _tooCloseToResourceGrid;

    public bool IsAnalysisComplete { get; private set; } = false;

    public IEnumerable<IExpandLocation> ExpandLocations { get; private set; } = Enumerable.Empty<IExpandLocation>();

    public ExpandAnalyzer(
        KnowledgeBase knowledgeBase,
        IFrameClock frameClock,
        ILogger logger,
        ISc2Client sc2Client,
        IGraphicalDebugger graphicalDebugger,
        ITerrainTracker terrainTracker,
        IResourceFinder resourceFinder,
        IPathfinder<Vector2> pathfinder,
        FootprintCalculator footprintCalculator
    ) {
        _knowledgeBase = knowledgeBase;
        _frameClock = frameClock;
        _logger = logger.CreateNamed("ExpandAnalyzer");
        _sc2Client = sc2Client;
        _graphicalDebugger = graphicalDebugger;
        _terrainTracker = terrainTracker;
        _resourceFinder = resourceFinder;
        _pathfinder = pathfinder;
        _footprintCalculator = footprintCalculator;
    }

    public void OnFrame(IGameState gameState) {
        if (IsAnalysisComplete) {
            return;
        }

        // For some reason querying for placement doesn't work before a few seconds after the game starts
        if (_frameClock.CurrentFrame < TimeUtils.SecsToFrames(5)) {
            return;
        }

        _logger.Info("Analyzing expand locations, this can take some time...");

        var resourceClusters = _resourceFinder.FindResourceClusters().ToList();
        _logger.Metric($"Found {resourceClusters.Count} resource clusters");

        var expandPositions = FindExpandPositions(gameState, resourceClusters).ToList();
        _logger.Metric($"Found {expandPositions.Count} expand positions");

        IsAnalysisComplete = expandPositions.Count == resourceClusters.Count;
        if (IsAnalysisComplete) {
            ExpandLocations = GenerateExpandLocations(gameState, expandPositions);

            // We don't need to save here because expands are saved in regions
            _logger.Success("Expand analysis done");
        }
        else {
            _logger.Error("Expand analysis failed, the number of resource clusters found does not match the number of expands found");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Finds the optimal expand location townhall positions by finding the closest 5x5 positions to each resource cluster.
    /// </summary>
    /// <param name="gameState">The current game state.</param>
    /// <param name="resourceClusters">The resource clusters that should represent expand locations.</param>
    /// <returns>A list of positions that represent the optimal townhall placement for each resource cluster.</returns>
    private IEnumerable<Vector2> FindExpandPositions(IGameState gameState, List<List<IUnit>> resourceClusters) {
        InitTooCloseToResourceGrid(gameState, resourceClusters);

        var expandLocations = new List<Vector2>();
        foreach (var resourceCluster in resourceClusters) {
            var clusterPositions = resourceCluster.Select(resource => resource.Position.ToVector2()).ToList();
            var centerPosition = Clustering.GetBoundingBoxCenter(clusterPositions).AsWorldGridCenter();
            var searchGrid = centerPosition.BuildSearchGrid(ExpandSearchRadius);

            var goodBuildSpot = searchGrid.FirstOrDefault(IsValidTownHallPlacement);
            if (goodBuildSpot != default) {
                expandLocations.Add(goodBuildSpot);
                _graphicalDebugger.AddSphere(_terrainTracker.WithWorldHeight(goodBuildSpot), KnowledgeBase.GameGridCellRadius, Colors.Green);
                _graphicalDebugger.AddSphere(_terrainTracker.WithWorldHeight(centerPosition), KnowledgeBase.GameGridCellRadius, Colors.Yellow);
            }
        }

        return expandLocations;
    }

    private void InitTooCloseToResourceGrid(IGameState gameState, IEnumerable<List<IUnit>> resourceClusters) {
        if (_tooCloseToResourceGrid != null) {
            return;
        }

        _tooCloseToResourceGrid = new List<List<bool>>();
        for (var x = 0; x < gameState.Terrain.MaxX; x++) {
            _tooCloseToResourceGrid.Add(new List<bool>(new bool[gameState.Terrain.MaxY]));
        }

        var cellsTooCloseToResource = resourceClusters.SelectMany(cluster => cluster)
            .SelectMany(_footprintCalculator.GetFootprint)
            .SelectMany(position => position.BuildSearchRadius(TooCloseToResourceDistance))
            .Select(position => position.AsWorldGridCorner());

        foreach (var cell in cellsTooCloseToResource) {
            if (DrawEnabled) {
                _graphicalDebugger.AddGridSquare(_terrainTracker.WithWorldHeight(cell.AsWorldGridCenter()), Colors.SunbrightOrange);
            }

            _tooCloseToResourceGrid[(int)cell.X][(int)cell.Y] = true;
        }
    }

    /// <summary>
    /// Determines if the suggested build spot is a valid townhall placement by checking if one could be placed there.
    /// </summary>
    /// <param name="buildSpot"></param>
    /// <returns></returns>
    private bool IsValidTownHallPlacement(Vector2 buildSpot) {
        if (_tooCloseToResourceGrid == null) {
            return false;
        }

        var footprint = _footprintCalculator.GetFootprint(buildSpot, UnitTypeId.Hatchery);
        var footprintIsClear = footprint.All(cell => !_tooCloseToResourceGrid[(int)cell.X][(int)cell.Y]);

        // We'll query just to be sure
        if (footprintIsClear && QueryPlacement(UnitTypeId.Hatchery, buildSpot) != ActionResult.CantBuildTooCloseToResources) {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Generates the ExpandLocations by associating locations and resource cluster, finding blockers and tagging the expand type properly.
    /// </summary>
    /// <param name="gameState">The current state of the game.</param>
    /// <param name="expandPositions">The positions of the found expand locations.</param>
    /// <returns>The ExpandLocations</returns>
    private IEnumerable<ExpandLocation> GenerateExpandLocations(IGameState gameState, IReadOnlyList<Vector2> expandPositions) {
        // Clusters
        var resourceClustersByExpand = new Dictionary<Vector2, HashSet<IUnit>>();
        foreach (var expandPosition in expandPositions) {
            resourceClustersByExpand[expandPosition] = _resourceFinder.FindExpandResources(expandPosition);
        }

        // Blockers
        var blockersByExpand = new Dictionary<Vector2, HashSet<IUnit>>();
        foreach (var expandPosition in expandPositions) {
            blockersByExpand[expandPosition] = _resourceFinder.FindExpandBlockers(expandPosition);
        }

        var expandTypes = new Dictionary<Vector2, ExpandType>();

        // ExpandType based on distance to own base
        var rank = 0;
        foreach (var expandPosition in expandPositions.OrderBy(expandPosition => _pathfinder.FindPath(expandPosition, gameState.StartingLocation)?.Count ?? int.MaxValue)) {
            var (expandType, newRank) = CalculateExpandType(resourceClustersByExpand[expandPosition], blockersByExpand[expandPosition], rank);
            expandTypes[expandPosition] = expandType;
            rank = newRank;
        }

        // ExpandType based on distance to enemy base
        rank = 0;
        foreach (var expandPosition in expandPositions.OrderBy(expandPosition => _pathfinder.FindPath(expandPosition, gameState.EnemyStartingLocation)?.Count ?? int.MaxValue)) {
            // Already set, skip
            if (expandTypes.ContainsKey(expandPosition) && expandTypes[expandPosition] != ExpandType.Far) {
                continue;
            }

            var (expandType, newRank) = CalculateExpandType(resourceClustersByExpand[expandPosition], blockersByExpand[expandPosition], rank);
            expandTypes[expandPosition] = expandType;
            rank = newRank;
        }

        return expandPositions
            .Select(expandPosition => new ExpandLocation(expandPosition, expandTypes[expandPosition], resourceClustersByExpand[expandPosition].ToList()))
            .ToList();
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="blockers"></param>
    /// <param name="resourceCluster"></param>
    /// <param name="rank"></param>
    /// <returns></returns>
    private static (ExpandType expandType, int newRank) CalculateExpandType(IReadOnlyCollection<IUnit> resourceCluster, IEnumerable<IUnit> blockers, int rank) {
        if (IsGoldExpand(resourceCluster)) {
            return (ExpandType.Gold, rank);
        }

        if (IsPocketExpand(resourceCluster, blockers)) {
            return (ExpandType.Pocket, rank);
        }

        return (GetExpandTypeByRank(rank), rank + 1);
    }

    /// <summary>
    /// A gold expand has gold minerals and/or purple gas
    /// </summary>
    /// <param name="resourceCluster">The resource cluster associated with the expand</param>
    /// <returns>True if the resource cluster associated with the expand contains rich resources</returns>
    private static bool IsGoldExpand(IEnumerable<IUnit> resourceCluster) {
        return resourceCluster.Any(resource => UnitTypeId.GoldMineralFields.Contains(resource.UnitType) || UnitTypeId.PurpleGasGeysers.Contains(resource.UnitType));
    }

    /// <summary>
    /// A pocket expand has less mineral patches or is blocked
    /// Gold expands also have less mineral patches, but they are not pocket expands
    /// </summary>
    /// <param name="resourceCluster">The resource cluster associated with the expand</param>
    /// <param name="blockers">The units blocking the expand</param>
    /// <returns>True if the expand is a pocket expand</returns>
    private static bool IsPocketExpand(IReadOnlyCollection<IUnit> resourceCluster, IEnumerable<IUnit> blockers) {
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

    private ActionResult QueryPlacement(uint buildingType, Vector2 position) {
        var queryBuildingPlacementResponse = _sc2Client.SendRequest(RequestBuilder.RequestQueryBuildingPlacement(buildingType, position, _knowledgeBase)).Result;
        if (queryBuildingPlacementResponse.Query.Placements.Count == 0) {
            return ActionResult.NotSupported;
        }

        if (queryBuildingPlacementResponse.Query.Placements.Count > 1) {
            _logger.Warning($"Expected 1 building placement result but got {queryBuildingPlacementResponse.Query.Placements.Count}");
        }

        return queryBuildingPlacementResponse.Query.Placements[0].Result;
    }
}
