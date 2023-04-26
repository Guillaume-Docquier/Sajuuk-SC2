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
    private readonly IEnemyRaceTracker _enemyRaceTracker;
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly IMapAnalyzer _mapAnalyzer;
    private readonly IExpandAnalyzer _expandAnalyzer;
    private readonly IRegionAnalyzer _regionAnalyzer;

    private const int TopPriority = 100;

    private IScoutingStrategy _concreteScoutingStrategy;
    private ScoutingTask _raceFindingScoutingTask;
    private bool _isInitialized = false;

    public RandomScoutingStrategy(
        IEnemyRaceTracker enemyRaceTracker,
        IVisibilityTracker visibilityTracker,
        IUnitsTracker unitsTracker,
        IMapAnalyzer mapAnalyzer,
        IExpandAnalyzer expandAnalyzer,
        IRegionAnalyzer regionAnalyzer
    ) {
        _enemyRaceTracker = enemyRaceTracker;
        _visibilityTracker = visibilityTracker;
        _unitsTracker = unitsTracker;
        _mapAnalyzer = mapAnalyzer;
        _expandAnalyzer = expandAnalyzer;
        _regionAnalyzer = regionAnalyzer;
    }

    public IEnumerable<ScoutingTask> GetNextScoutingTasks() {
        if (_enemyRaceTracker.EnemyRace != Race.Random) {
            if (_concreteScoutingStrategy == null) {
                _concreteScoutingStrategy = ScoutingStrategyFactory.CreateNew(_enemyRaceTracker, _visibilityTracker, _unitsTracker, _mapAnalyzer, _expandAnalyzer, _regionAnalyzer);

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
            var enemyMain = _expandAnalyzer.GetExpand(Alliance.Enemy, ExpandType.Main);
            _raceFindingScoutingTask = new ExpandScoutingTask(_visibilityTracker, _unitsTracker, _mapAnalyzer, enemyMain.Position, TopPriority, maxScouts: 1);

            return new [] { _raceFindingScoutingTask };
        }

        return Enumerable.Empty<ScoutingTask>();
    }

    /// <summary>
    /// Init a task to get vision of the enemy natural to identify the enemy race
    /// </summary>
    private void Init() {
        var enemyNatural = _expandAnalyzer.GetExpand(Alliance.Enemy, ExpandType.Natural);
        _raceFindingScoutingTask = new ExpandScoutingTask(_visibilityTracker, _unitsTracker, _mapAnalyzer, enemyNatural.Position, TopPriority, maxScouts: 1);

        _isInitialized = true;
    }
}
