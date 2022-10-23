using System.Collections.Generic;
using System.Linq;
using Bot.Managers.ScoutManagement.ScoutingTasks;
using SC2APIProtocol;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

public class RandomScoutingStrategy : IScoutingStrategy {
    private IScoutingStrategy _concreteScoutingStrategy;

    public IEnumerable<ScoutingTask> Execute() {
        if (Controller.EnemyRace != Race.Random) {
            _concreteScoutingStrategy = ScoutingStrategyFactory.CreateNew(Controller.EnemyRace);
        }

        if (_concreteScoutingStrategy != null) {
            return _concreteScoutingStrategy.Execute();
        }

        // TODO GD Do some scouting until we spot the enemy
        return Enumerable.Empty<ScoutingTask>();
    }
}
