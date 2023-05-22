using System.Collections.Generic;
using Bot.GameSense;
using Bot.Managers.ScoutManagement.ScoutingTasks;
using Bot.MapAnalysis.ExpandAnalysis;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

/// <summary>
/// The Protoss scouting strategy will scout our natural first to spot canon rushes and then the enemy natural.
/// For now, nothing more because saving the overlords requires pillar knowledge and air safety analysis.
/// </summary>
public class ProtossScoutingStrategy : IScoutingStrategy {
    private readonly IRegionsTracker _regionsTracker;
    private readonly IScoutingTaskFactory _scoutingTaskFactory;
    private readonly IFrameClock _frameClock;

    private const int TopPriority = 100;

    private bool _isInitialized = false;

    private IExpandLocation _ownNatural;
    private ScoutingTask _ownNaturalScoutingTask;
    private ScoutingTask _enemyNaturalScoutingTask;

    public ProtossScoutingStrategy(
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

            yield return _ownNaturalScoutingTask;
            yield return _enemyNaturalScoutingTask;
        }

        // Keep watching
        if (_ownNaturalScoutingTask.IsComplete()) {
            _ownNaturalScoutingTask = _scoutingTaskFactory.CreateRegionScoutingTask(_ownNatural.Region, priority: TopPriority, maxScouts: 1);

            yield return _ownNaturalScoutingTask;
        }
    }

    private void Init() {
        _ownNatural = _regionsTracker.GetExpand(Alliance.Self, ExpandType.Natural);
        _ownNaturalScoutingTask = _scoutingTaskFactory.CreateRegionScoutingTask(_ownNatural.Region, priority: TopPriority, maxScouts: 1);

        var enemyNatural = _regionsTracker.GetExpand(Alliance.Enemy, ExpandType.Natural);
        _enemyNaturalScoutingTask = _scoutingTaskFactory.CreateExpandScoutingTask(enemyNatural.Position, priority: TopPriority - 1, maxScouts: 1);

        _isInitialized = true;
    }
}
