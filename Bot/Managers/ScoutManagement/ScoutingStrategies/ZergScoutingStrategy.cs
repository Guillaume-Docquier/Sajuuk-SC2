using System.Collections.Generic;
using System.Linq;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers.ScoutManagement.ScoutingTasks;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

public class ZergScoutingStrategy : IScoutingStrategy {
    private const int TopPriority = 100;
    private static HashSet<uint> _threateningUnitTypes = new HashSet<uint>
    {
        Units.Mutalisk,
        Units.Corruptor,
        Units.Ravager, // TODO Avoid biles instead
        Units.Hydralisk,
    };

    private readonly ScoutingTask _naturalScoutingTask;
    private readonly ScoutingTask _naturalExitVisibilityTask;
    private readonly ScoutingTask _thirdScoutingTask;
    private readonly ScoutingTask _fourthScoutingTask;

    public ZergScoutingStrategy() {
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
    }

    public IEnumerable<ScoutingTask> Execute() {
        if (Controller.Frame == 0) {
            yield return _naturalScoutingTask;
            yield return _naturalExitVisibilityTask;
            yield return _thirdScoutingTask;
            yield return _fourthScoutingTask;
        }

        // TODO GD Decide based on amount of dead units instead?
        if (UnitsTracker.EnemyUnits.Any(unit => _threateningUnitTypes.Contains(unit.UnitType))) {
            _naturalScoutingTask.Cancel();
            _naturalExitVisibilityTask.Cancel();
            _thirdScoutingTask.Cancel();
            _fourthScoutingTask.Cancel();
        }
    }
}
