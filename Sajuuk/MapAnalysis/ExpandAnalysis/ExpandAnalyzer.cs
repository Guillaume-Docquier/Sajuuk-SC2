using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Sajuuk.Algorithms;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.Utils;
using SC2APIProtocol;

namespace Sajuuk.MapAnalysis.ExpandAnalysis;

public class ExpandAnalyzer : IExpandAnalyzer, INeedUpdating {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IBuildingTracker _buildingTracker;
    private readonly IExpandUnitsAnalyzer _expandUnitsAnalyzer;
    private readonly IFrameClock _frameClock;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IClustering _clustering;
    private readonly IPathfinder _pathfinder;

    private readonly FootprintCalculator _footprintCalculator;

    private const bool DrawEnabled = false;
    private const int TypicalResourceCount = 10;
    private const int ExpandSearchRadius = 5;
    private static readonly float TooCloseToResourceDistance = (float)Math.Sqrt(1*1 + 3*3); // Empirical, 1x3 diagonal

    private List<List<bool>> _tooCloseToResourceGrid;
    private List<ExpandLocation> _expandLocations;

    public bool IsAnalysisComplete { get; private set; }  = false;
    public IEnumerable<ExpandLocation> ExpandLocations => _expandLocations;

    public ExpandAnalyzer(
        ITerrainTracker terrainTracker,
        IBuildingTracker buildingTracker,
        IExpandUnitsAnalyzer expandUnitsAnalyzer,
        IFrameClock frameClock,
        IGraphicalDebugger graphicalDebugger,
        IClustering clustering,
        IPathfinder pathfinder
    ) {
        _terrainTracker = terrainTracker;
        _buildingTracker = buildingTracker;
        _expandUnitsAnalyzer = expandUnitsAnalyzer;
        _frameClock = frameClock;
        _graphicalDebugger = graphicalDebugger;
        _clustering = clustering;
        _pathfinder = pathfinder;

        // TODO GD Inject that, probably?
        _footprintCalculator = new FootprintCalculator(_terrainTracker);
    }

    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        if (IsAnalysisComplete) {
            return;
        }

        // For some reason querying for placement doesn't work before a few seconds after the game starts
        if (_frameClock.CurrentFrame < TimeUtils.SecsToFrames(5)) {
            return;
        }

        Logger.Info("Analyzing expand locations, this can take some time...");

        var resourceClusters = _expandUnitsAnalyzer.FindResourceClusters().ToList();
        Logger.Metric("Found {0} resource clusters", resourceClusters.Count);

        var expandPositions = FindExpandLocations(resourceClusters).ToList();
        Logger.Metric("Found {0} expand locations", expandPositions.Count);

        IsAnalysisComplete = expandPositions.Count == resourceClusters.Count;
        if (IsAnalysisComplete) {
            _expandLocations = GenerateExpandLocations(expandPositions);
            // TODO GD We could save here, but we don't need to because expands are saved in regions
            Logger.Success("Expand analysis done");
        }
        else {
            Logger.Error("Expand analysis failed, the number of resource clusters found does not match the number of expands found");
            Environment.Exit(1);
        }
    }

    private IEnumerable<Vector2> FindExpandLocations(List<List<Unit>> resourceClusters) {
        InitTooCloseToResourceGrid(resourceClusters);

        var expandLocations = new List<Vector2>();
        foreach (var resourceCluster in resourceClusters) {
            var centerPosition = _clustering.GetBoundingBoxCenter(resourceCluster).AsWorldGridCenter().ToVector2();
            var searchGrid = _terrainTracker.BuildSearchGrid(centerPosition, gridRadius: ExpandSearchRadius);

            var goodBuildSpot = searchGrid.FirstOrDefault(IsValidExpandPlacement);
            if (goodBuildSpot != default) {
                expandLocations.Add(goodBuildSpot);
                _graphicalDebugger.AddSphere(_terrainTracker.WithWorldHeight(goodBuildSpot), KnowledgeBase.GameGridCellRadius, Colors.Green);
                _graphicalDebugger.AddSphere(_terrainTracker.WithWorldHeight(centerPosition), KnowledgeBase.GameGridCellRadius, Colors.Yellow);
            }
        }

        return expandLocations;
    }

    private void InitTooCloseToResourceGrid(IEnumerable<List<Unit>> resourceClusters) {
        if (_tooCloseToResourceGrid != null) {
            return;
        }

        _tooCloseToResourceGrid = new List<List<bool>>();
        for (var x = 0; x < _terrainTracker.MaxX; x++) {
            _tooCloseToResourceGrid.Add(new List<bool>(new bool[_terrainTracker.MaxY]));
        }

        var cellsTooCloseToResource = resourceClusters.SelectMany(cluster => cluster)
            .SelectMany(_footprintCalculator.GetFootprint)
            .SelectMany(position => _terrainTracker.BuildSearchRadius(position, TooCloseToResourceDistance))
            .Select(position => position.AsWorldGridCorner());

        foreach (var cell in cellsTooCloseToResource) {
            if (DrawEnabled) {
                _graphicalDebugger.AddGridSquare(_terrainTracker.WithWorldHeight(cell.AsWorldGridCenter()), Colors.SunbrightOrange);
            }

            _tooCloseToResourceGrid[(int)cell.X][(int)cell.Y] = true;
        }
    }

    private bool IsValidExpandPlacement(Vector2 buildSpot) {
        var footprint = _terrainTracker.GetBuildingFootprint(buildSpot, Units.Hatchery);
        var footprintIsClear = footprint.All(cell => !_tooCloseToResourceGrid[(int)cell.X][(int)cell.Y]);

        // We'll query just to be sure
        if (footprintIsClear && _buildingTracker.QueryPlacement(Units.Hatchery, buildSpot) != ActionResult.CantBuildTooCloseToResources) {
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
    private List<ExpandLocation> GenerateExpandLocations(IReadOnlyList<Vector2> expandPositions) {
        // Clusters
        var resourceClustersByExpand = new Dictionary<Vector2, HashSet<Unit>>();
        foreach (var expandPosition in expandPositions) {
            resourceClustersByExpand[expandPosition] = _expandUnitsAnalyzer.FindExpandResources(expandPosition);
        }

        // Blockers
        var blockersByExpand = new Dictionary<Vector2, HashSet<Unit>>();
        foreach (var expandPosition in expandPositions) {
            blockersByExpand[expandPosition] = _expandUnitsAnalyzer.FindExpandBlockers(expandPosition);
        }

        var expandTypes = new Dictionary<Vector2, ExpandType>();

        // ExpandType based on distance to own base
        var rank = 0;
        foreach (var expandPosition in expandPositions.OrderBy(expandPosition => _pathfinder.FindPath(expandPosition, _terrainTracker.StartingLocation).Count)) {
            var (expandType, newRank) = CalculateExpandType(resourceClustersByExpand[expandPosition], blockersByExpand[expandPosition], rank);
            expandTypes[expandPosition] = expandType;
            rank = newRank;
        }

        // ExpandType based on distance to enemy base
        rank = 0;
        foreach (var expandPosition in expandPositions.OrderBy(expandPosition => _pathfinder.FindPath(expandPosition, _terrainTracker.EnemyStartingLocation).Count)) {
            // Already set, skip
            if (expandTypes.ContainsKey(expandPosition) && expandTypes[expandPosition] != ExpandType.Far) {
                continue;
            }

            var (expandType, newRank) = CalculateExpandType(resourceClustersByExpand[expandPosition], blockersByExpand[expandPosition], rank);
            expandTypes[expandPosition] = expandType;
            rank = newRank;
        }

        return expandPositions
            .Select(expandPosition => new ExpandLocation(expandPosition, expandTypes[expandPosition], resourceClustersByExpand[expandPosition]))
            .ToList();
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
    /// A gold expand has gold minerals and/or purple gas
    /// </summary>
    /// <param name="resourceCluster">The resource cluster associated with the expand</param>
    /// <returns>True if the resource cluster associated with the expand contains rich resources</returns>
    private static bool IsGoldExpand(IEnumerable<Unit> resourceCluster) {
        return resourceCluster.Any(resource => Units.GoldMineralFields.Contains(resource.UnitType) || Units.PurpleGasGeysers.Contains(resource.UnitType));
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
