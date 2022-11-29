using System.Collections.Generic;
using System.Linq;
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

    public IEnumerable<ScoutingTask> Execute() {
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
        if (UnitsTracker.EnemyUnits.Any(unit => ThreateningUnitTypes.Contains(unit.UnitType))) {
            _naturalScoutingTask.Cancel();
            _naturalExitVisibilityTask.Cancel();
            _thirdScoutingTask.Cancel();
            _fourthScoutingTask.Cancel();
        }
    }

    private void Init() {
        var enemyNaturalExpandLocation = ExpandAnalyzer.GetExpand(Alliance.Enemy, ExpandType.Natural);
        _naturalScoutingTask = new ExpandScoutingTask(enemyNaturalExpandLocation.Position, TopPriority, maxScouts: 1, waitForExpand: true);

        var enemyNaturalExitRegion = RegionAnalyzer.GetNaturalExitRegion(Alliance.Enemy);
        var enemyNaturalExitRegionRamps = enemyNaturalExitRegion.Neighbors
            .Select(neighbor => neighbor.Region)
            .Where(region => region.Type == RegionType.Ramp)
            .ToList();

        var cellsToMaintainVisible = enemyNaturalExitRegion.Cells
            .Concat(enemyNaturalExitRegionRamps.SelectMany(region => region.Cells))
            .ToList();

        _naturalExitVisibilityTask = new MaintainVisibilityScoutingTask(cellsToMaintainVisible, priority: TopPriority - 1, enemyNaturalExitRegionRamps.Count);

        var enemyThirdExpandLocation = ExpandAnalyzer.GetExpand(Alliance.Enemy, ExpandType.Third);
        _thirdScoutingTask = new ExpandScoutingTask(enemyThirdExpandLocation.Position, TopPriority - 2, maxScouts: 1, waitForExpand: true);

        var enemyFourthExpandLocation = ExpandAnalyzer.GetExpand(Alliance.Enemy, ExpandType.Fourth);
        _fourthScoutingTask = new ExpandScoutingTask(enemyFourthExpandLocation.Position, TopPriority - 2, maxScouts: 1, waitForExpand: true);

        _isInitialized = true;
    }
}
