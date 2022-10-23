using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.Managers.ScoutManagement.ScoutingTasks;
using Bot.MapKnowledge;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

public class ZergScoutingStrategy : IScoutingStrategy {
    private readonly ScoutingTask _naturalScoutingTask;
    private readonly ScoutingTask _naturalExitVisibilityTask;

    public ZergScoutingStrategy() {
        var enemyNaturalExpandLocation = ExpandAnalyzer.ExpandLocations
            .Where(expandLocation => expandLocation.ExpandType == ExpandType.Natural)
            .MinBy(expandLocation => expandLocation.Position.HorizontalDistanceTo(MapAnalyzer.EnemyStartingLocation))!;

        _naturalScoutingTask = new ExpandScoutingTask(enemyNaturalExpandLocation.Position);
    }

    public IEnumerable<ScoutingTask> Execute() {
        if (Controller.Frame == 0) {
            yield return _naturalScoutingTask;
        }
    }
}
