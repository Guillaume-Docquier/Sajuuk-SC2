using System.Collections.Generic;
using System.Linq;
using Bot.GameSense;
using Bot.Managers.ScoutManagement.ScoutingTasks;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

/// <summary>
/// The RandomScoutingStrategy will try to identify the enemy race.
/// It will use the proper Race scouting strategy once it knows the enemy race.
/// </summary>
public class RandomScoutingStrategy : IScoutingStrategy {
    private const int TopPriority = 100;

    private readonly IEnemyRaceTracker _enemyRaceTracker;

    private IScoutingStrategy _concreteScoutingStrategy;
    private ScoutingTask _raceFindingScoutingTask;
    private bool _isInitialized = false;

    public RandomScoutingStrategy(IEnemyRaceTracker enemyRaceTracker) {
        _enemyRaceTracker = enemyRaceTracker;
    }

    public IEnumerable<ScoutingTask> GetNextScoutingTasks() {
        if (_enemyRaceTracker.EnemyRace != Race.Random) {
            if (_concreteScoutingStrategy == null) {
                _concreteScoutingStrategy = ScoutingStrategyFactory.CreateNew(_enemyRaceTracker);

                // Cancel our task, we will rely on the concrete scouting strategy now
                _raceFindingScoutingTask.Cancel();
            }

            return _concreteScoutingStrategy.GetNextScoutingTasks();
        }

        if (!_isInitialized) {
            Init();

            return new [] { _raceFindingScoutingTask };
        }

        // Go for the main base if nothing in the natural
        if (_raceFindingScoutingTask.IsComplete()) {
            var enemyMain = ExpandAnalyzer.GetExpand(Alliance.Enemy, ExpandType.Main);
            _raceFindingScoutingTask = new ExpandScoutingTask(enemyMain.Position, TopPriority, maxScouts: 1);

            return new [] { _raceFindingScoutingTask };
        }

        return Enumerable.Empty<ScoutingTask>();
    }

    /// <summary>
    /// Init a task to get vision of the enemy natural to identify the enemy race
    /// </summary>
    private void Init() {
        var enemyNatural = ExpandAnalyzer.GetExpand(Alliance.Enemy, ExpandType.Natural);
        _raceFindingScoutingTask = new ExpandScoutingTask(enemyNatural.Position, TopPriority, maxScouts: 1);

        _isInitialized = true;
    }
}
