using System.Collections.Generic;
using Bot.GameSense;
using Bot.Managers.ScoutManagement.ScoutingTasks;
using Bot.MapKnowledge;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

/// <summary>
/// The Protoss scouting strategy will scout our natural first to spot canon rushes and then the enemy natural.
/// For now, nothing more because saving the overlords requires pillar knowledge and air safety analysis.
/// </summary>
public class ProtossScoutingStrategy : IScoutingStrategy {
    private const int TopPriority = 100;

    private readonly IVisibilityTracker _visibilityTracker;

    private bool _isInitialized = false;

    private ExpandLocation _ownNatural;
    private ScoutingTask _ownNaturalScoutingTask;
    private ScoutingTask _enemyNaturalScoutingTask;

    public ProtossScoutingStrategy(IVisibilityTracker visibilityTracker) {
        _visibilityTracker = visibilityTracker;
    }

    public IEnumerable<ScoutingTask> GetNextScoutingTasks() {
        if (!ExpandAnalyzer.IsInitialized || !RegionAnalyzer.IsInitialized) {
            yield break;
        }

        // Stop after 4 minutes just in case to avoid sending overlords to their death
        // We'll need smarter checks to determine if we're safe
        if (Controller.Frame >= TimeUtils.SecsToFrames(4 * 60)) {
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
            _ownNaturalScoutingTask = new RegionScoutingTask(_visibilityTracker, _ownNatural.Position, priority: TopPriority, maxScouts: 1);

            yield return _ownNaturalScoutingTask;
        }
    }

    private void Init() {
        _ownNatural = ExpandAnalyzer.GetExpand(Alliance.Self, ExpandType.Natural);
        _ownNaturalScoutingTask = new RegionScoutingTask(_visibilityTracker, _ownNatural.Position, priority: TopPriority, maxScouts: 1);

        var enemyNatural = ExpandAnalyzer.GetExpand(Alliance.Enemy, ExpandType.Natural);
        _enemyNaturalScoutingTask = new ExpandScoutingTask(_visibilityTracker, enemyNatural.Position, priority: TopPriority - 1, maxScouts: 1);

        _isInitialized = true;
    }
}
