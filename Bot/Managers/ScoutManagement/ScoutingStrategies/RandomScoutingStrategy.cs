using System.Collections.Generic;
using System.Linq;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameSense;
using Bot.Managers.ScoutManagement.ScoutingTasks;
using Bot.MapAnalysis.ExpandAnalysis;
using SC2APIProtocol;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

/// <summary>
/// The RandomScoutingStrategy will try to identify the enemy race.
/// It will use the proper Race scouting strategy once it knows the enemy race.
/// </summary>
public class RandomScoutingStrategy : IScoutingStrategy {
    private readonly IEnemyRaceTracker _enemyRaceTracker;
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IScoutingTaskFactory _scoutingTaskFactory;

    private const int TopPriority = 100;

    private IScoutingStrategy _concreteScoutingStrategy;
    private ScoutingTask _raceFindingScoutingTask;
    private bool _isInitialized = false;

    public RandomScoutingStrategy(
        IEnemyRaceTracker enemyRaceTracker,
        IVisibilityTracker visibilityTracker,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        IGraphicalDebugger graphicalDebugger,
        IScoutingTaskFactory scoutingTaskFactory
    ) {
        _enemyRaceTracker = enemyRaceTracker;
        _visibilityTracker = visibilityTracker;
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _regionsTracker = regionsTracker;
        _graphicalDebugger = graphicalDebugger;
        _scoutingTaskFactory = scoutingTaskFactory;
    }

    public IEnumerable<ScoutingTask> GetNextScoutingTasks() {
        if (_enemyRaceTracker.EnemyRace != Race.Random) {
            if (_concreteScoutingStrategy == null) {
                _concreteScoutingStrategy = ScoutingStrategyFactory.CreateNew(_enemyRaceTracker, _visibilityTracker, _unitsTracker, _terrainTracker, _regionsTracker, _graphicalDebugger, _scoutingTaskFactory);

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
