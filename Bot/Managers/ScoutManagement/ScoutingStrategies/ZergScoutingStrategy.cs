using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers.ScoutManagement.ScoutingTasks;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

/// <summary>
/// The Zerg scouting strategy scouts the enemy natural, then holds vision just outside of it.
/// It also scouts the 3rd and 4th afterwards.
/// Overlords are recalled if dangerous ground to air units are detected.
/// </summary>
public class ZergScoutingStrategy : IScoutingStrategy {
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IUnitsTracker _unitsTracker;

    private const int TopPriority = 100;

    private bool _isInitialized = false;

    private static readonly HashSet<uint> ThreateningUnitTypes = new HashSet<uint>
    {
        Units.Mutalisk,
        Units.Corruptor,
        Units.Ravager, // TODO Avoid biles instead
        Units.Hydralisk,
    };

    private ScoutingTask _naturalScoutingTask;
    private ScoutingTask _naturalExitVisibilityTask;
    private ScoutingTask _thirdScoutingTask;
    private ScoutingTask _fourthScoutingTask;

    public ZergScoutingStrategy(IVisibilityTracker visibilityTracker, IUnitsTracker unitsTracker) {
        _visibilityTracker = visibilityTracker;
        _unitsTracker = unitsTracker;
    }

    public IEnumerable<ScoutingTask> GetNextScoutingTasks() {
        if (!ExpandAnalyzer.IsInitialized || !RegionAnalyzer.IsInitialized) {
            yield break;
        }

        if (!_isInitialized) {
            Init();

            yield return _naturalScoutingTask;
            yield return _naturalExitVisibilityTask;
            yield return _thirdScoutingTask;
            yield return _fourthScoutingTask;
        }

        // TODO GD Decide based on amount of dead units instead?
        if (_unitsTracker.EnemyUnits.Any(unit => ThreateningUnitTypes.Contains(unit.UnitType))) {
            _naturalScoutingTask.Cancel();
            _naturalExitVisibilityTask.Cancel();
            _thirdScoutingTask.Cancel();
            _fourthScoutingTask.Cancel();
        }

        if (_naturalScoutingTask.IsComplete()) {
            _naturalExitVisibilityTask.Priority = _naturalScoutingTask.Priority;
        }
    }

    private void Init() {
        var enemyNaturalExpandLocation = ExpandAnalyzer.GetExpand(Alliance.Enemy, ExpandType.Natural);
        _naturalScoutingTask = new ExpandScoutingTask(_visibilityTracker, _unitsTracker, enemyNaturalExpandLocation.Position, TopPriority, maxScouts: 1, waitForExpand: true);

        var enemyNaturalExitRegion = RegionAnalyzer.GetNaturalExitRegion(Alliance.Enemy);
        var enemyNaturalExitRegionRamps = enemyNaturalExitRegion.Neighbors
            .Select(neighbor => neighbor.Region)
            .Where(region => region.Type == RegionType.Ramp)
            .ToList();

        var cellsToMaintainVisible = enemyNaturalExitRegion.Cells
            .Concat(enemyNaturalExitRegionRamps.SelectMany(region => region.Cells))
            .ToList();

        _naturalExitVisibilityTask = new MaintainVisibilityScoutingTask(_visibilityTracker, cellsToMaintainVisible, priority: TopPriority - 3, enemyNaturalExitRegionRamps.Count);

        var enemyThirdExpandLocation = ExpandAnalyzer.GetExpand(Alliance.Enemy, ExpandType.Third);
        _thirdScoutingTask = new ExpandScoutingTask(_visibilityTracker, _unitsTracker, enemyThirdExpandLocation.Position, TopPriority - 1, maxScouts: 1, waitForExpand: true);

        var enemyFourthExpandLocation = ExpandAnalyzer.GetExpand(Alliance.Enemy, ExpandType.Fourth);
        _fourthScoutingTask = new ExpandScoutingTask(_visibilityTracker, _unitsTracker, enemyFourthExpandLocation.Position, TopPriority - 2, maxScouts: 1, waitForExpand: true);

        // We want to route an overlord towards the center of the map sooner than the edge
        // Most early attacks will route through the center, we want to see them
        // The expand that's closest to the main is usually towards the center of the map
        var enemyMainPosition = ExpandAnalyzer.GetExpand(Alliance.Enemy, ExpandType.Main).Position;
        if (_thirdScoutingTask.ScoutLocation.DistanceTo(enemyMainPosition) > _fourthScoutingTask.ScoutLocation.DistanceTo(enemyMainPosition)) {
            _thirdScoutingTask.Priority--;
            _fourthScoutingTask.Priority++;
        }

        _isInitialized = true;
    }
}
