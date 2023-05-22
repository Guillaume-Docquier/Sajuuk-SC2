using System.Collections.Generic;
using Bot.GameSense;
using Bot.Managers.ScoutManagement.ScoutingTasks;
using Bot.MapAnalysis.ExpandAnalysis;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

/// <summary>
/// The Terran scouting strategy will scout the enemy natural for some early game hints.
/// For now, nothing more because saving the overlords requires pillar knowledge and air safety analysis.
/// </summary>
public class TerranScoutingStrategy : IScoutingStrategy {
    private readonly IRegionsTracker _regionsTracker;
    private readonly IScoutingTaskFactory _scoutingTaskFactory;
    private readonly IFrameClock _frameClock;

    private const int TopPriority = 100;

    private bool _isInitialized = false;

    private ScoutingTask _enemyNaturalScoutingTask;

    public TerranScoutingStrategy(
        IRegionsTracker regionsTracker,
        IScoutingTaskFactory scoutingTaskFactory,
        IFrameClock frameClock
    ) {
        _regionsTracker = regionsTracker;
        _scoutingTaskFactory = scoutingTaskFactory;
        _frameClock = frameClock;
    }

    public IEnumerable<ScoutingTask> GetNextScoutingTasks() {
        // Stop after 4 minutes just in case to avoid sending overlords to their death
        // We'll need smarter checks to determine if we're safe
        if (_frameClock.CurrentFrame >= TimeUtils.SecsToFrames(4 * 60)) {
            if (_isInitialized) {
                _enemyNaturalScoutingTask.Cancel();
            }

            yield break;
        }

        if (!_isInitialized) {
            Init();

            yield return _enemyNaturalScoutingTask;
        }
    }

    private void Init() {
        var enemyNatural = _regionsTracker.GetExpand(Alliance.Enemy, ExpandType.Natural);
        _enemyNaturalScoutingTask = _scoutingTaskFactory.CreateExpandScoutingTask(enemyNatural.Position, TopPriority, maxScouts: 1);

        _isInitialized = true;
    }
}
