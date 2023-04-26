using System.Collections.Generic;
using Bot.GameSense;
using Bot.Managers.ScoutManagement.ScoutingTasks;
using Bot.MapKnowledge;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

/// <summary>
/// The Terran scouting strategy will scout the enemy natural for some early game hints.
/// For now, nothing more because saving the overlords requires pillar knowledge and air safety analysis.
/// </summary>
public class TerranScoutingStrategy : IScoutingStrategy {
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly IMapAnalyzer _mapAnalyzer;
    private readonly IExpandAnalyzer _expandAnalyzer;
    private readonly IRegionAnalyzer _regionAnalyzer;

    private const int TopPriority = 100;

    private bool _isInitialized = false;

    private ScoutingTask _enemyNaturalScoutingTask;

    public TerranScoutingStrategy(
        IVisibilityTracker visibilityTracker,
        IUnitsTracker unitsTracker,
        IMapAnalyzer mapAnalyzer,
        IExpandAnalyzer expandAnalyzer,
        IRegionAnalyzer regionAnalyzer
    ) {
        _visibilityTracker = visibilityTracker;
        _unitsTracker = unitsTracker;
        _mapAnalyzer = mapAnalyzer;
        _expandAnalyzer = expandAnalyzer;
        _regionAnalyzer = regionAnalyzer;
    }

    public IEnumerable<ScoutingTask> GetNextScoutingTasks() {
        if (!_expandAnalyzer.IsInitialized || !_regionAnalyzer.IsInitialized) {
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

            yield return _enemyNaturalScoutingTask;
        }
    }

    private void Init() {
        var enemyNatural = _expandAnalyzer.GetExpand(Alliance.Enemy, ExpandType.Natural);
        _enemyNaturalScoutingTask = new ExpandScoutingTask(_visibilityTracker, _unitsTracker, _mapAnalyzer, enemyNatural.Position, TopPriority, maxScouts: 1);

        _isInitialized = true;
    }
}
