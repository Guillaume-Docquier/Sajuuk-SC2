using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.Managers.ScoutManagement.ScoutingTasks;
using Bot.MapKnowledge;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

public class ZergScoutingStrategy : IScoutingStrategy {
    private const int TopPriority = 100;

    private readonly ScoutingTask _naturalScoutingTask;
    private readonly ScoutingTask _naturalExitVisibilityTask;
    private readonly ScoutingTask _thirdScoutingTask;
    private readonly ScoutingTask _fourthScoutingTask;

    public ZergScoutingStrategy() {
        var enemyNaturalExpandLocation = ExpandAnalyzer.ExpandLocations
            .Where(expandLocation => expandLocation.ExpandType == ExpandType.Natural)
            .MinBy(expandLocation => expandLocation.Position.HorizontalDistanceTo(MapAnalyzer.EnemyStartingLocation))!;

        _naturalScoutingTask = new ExpandScoutingTask(enemyNaturalExpandLocation.Position, TopPriority, maxScouts: 1, waitForExpand: true);

        var enemyNaturalExitRegion = RegionAnalyzer.Regions
            .Where(region => region.Type == RegionType.OpenArea)
            .MinBy(region => region.Center.HorizontalDistanceTo(enemyNaturalExpandLocation.Position))!;

        var enemyNaturalExitRegionRamps = enemyNaturalExitRegion.Neighbors
            .Select(neighbor => neighbor.Region)
            .Where(region => region.Type == RegionType.Ramp)
            .ToList();

        var cellsToMaintainVisible = enemyNaturalExitRegion.Cells
            .Concat(enemyNaturalExitRegionRamps.SelectMany(region => region.Cells))
            .ToList();

        _naturalExitVisibilityTask = new MaintainVisibilityScoutingTask(cellsToMaintainVisible, priority: TopPriority - 1, enemyNaturalExitRegionRamps.Count);

        // TODO GD Make ExpandAnalyzer.GetExpand(Alliance, ExpandType)
        var enemyThirdExpandLocation = ExpandAnalyzer.ExpandLocations
            .Where(expandLocation => expandLocation.ExpandType == ExpandType.Third)
            .MinBy(expandLocation => expandLocation.Position.HorizontalDistanceTo(MapAnalyzer.EnemyStartingLocation))!;

        _thirdScoutingTask = new ExpandScoutingTask(enemyThirdExpandLocation.Position, TopPriority - 2, maxScouts: 1, waitForExpand: true);

        var enemyFourthExpandLocation = ExpandAnalyzer.ExpandLocations
            .Where(expandLocation => expandLocation.ExpandType == ExpandType.Fourth)
            .MinBy(expandLocation => expandLocation.Position.HorizontalDistanceTo(MapAnalyzer.EnemyStartingLocation))!;

        _fourthScoutingTask = new ExpandScoutingTask(enemyFourthExpandLocation.Position, TopPriority - 2, maxScouts: 1, waitForExpand: true);
    }

    public IEnumerable<ScoutingTask> Execute() {
        if (Controller.Frame == 0) {
            yield return _naturalScoutingTask;
            yield return _naturalExitVisibilityTask;
            yield return _thirdScoutingTask;
            yield return _fourthScoutingTask;
        }
    }
}
