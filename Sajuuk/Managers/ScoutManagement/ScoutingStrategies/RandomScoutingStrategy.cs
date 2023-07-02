using System.Collections.Generic;
using System.Linq;
using Sajuuk.GameSense;
using Sajuuk.Managers.ScoutManagement.ScoutingTasks;
using Sajuuk.MapAnalysis.ExpandAnalysis;
using SC2APIProtocol;

namespace Sajuuk.Managers.ScoutManagement.ScoutingStrategies;

/// <summary>
/// The RandomScoutingStrategy will try to identify the enemy race.
/// It will use the proper Race scouting strategy once it knows the enemy race.
/// </summary>
public class RandomScoutingStrategy : IScoutingStrategy {
    private readonly IEnemyRaceTracker _enemyRaceTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IScoutingTaskFactory _scoutingTaskFactory;
    private readonly IScoutingStrategyFactory _scoutingStrategyFactory;

    private const int TopPriority = 100;

    private IScoutingStrategy _concreteScoutingStrategy;
    private ScoutingTask _raceFindingScoutingTask;
    private bool _isInitialized = false;

    public RandomScoutingStrategy(
        IEnemyRaceTracker enemyRaceTracker,
        IRegionsTracker regionsTracker,
        IScoutingTaskFactory scoutingTaskFactory,
        IScoutingStrategyFactory scoutingStrategyFactory
    ) {
        _enemyRaceTracker = enemyRaceTracker;
        _regionsTracker = regionsTracker;
        _scoutingTaskFactory = scoutingTaskFactory;
        _scoutingStrategyFactory = scoutingStrategyFactory;
    }

    public IEnumerable<ScoutingTask> GetNextScoutingTasks() {
        if (_enemyRaceTracker.EnemyRace != Race.Random) {
            if (_concreteScoutingStrategy == null) {
                _concreteScoutingStrategy = _scoutingStrategyFactory.CreateNew(_enemyRaceTracker.EnemyRace);

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
            var enemyMain = _regionsTracker.GetExpand(Alliance.Enemy, ExpandType.Main);
            _raceFindingScoutingTask = _scoutingTaskFactory.CreateExpandScoutingTask(enemyMain.Position, TopPriority, maxScouts: 1);

            return new [] { _raceFindingScoutingTask };
        }

        return Enumerable.Empty<ScoutingTask>();
    }

    /// <summary>
    /// Init a task to get vision of the enemy natural to identify the enemy race
    /// </summary>
    private void Init() {
        var enemyNatural = _regionsTracker.GetExpand(Alliance.Enemy, ExpandType.Natural);
        _raceFindingScoutingTask = _scoutingTaskFactory.CreateExpandScoutingTask(enemyNatural.Position, TopPriority, maxScouts: 1);

        _isInitialized = true;
    }
}
