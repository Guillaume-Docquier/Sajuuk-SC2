using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.Managers.ScoutManagement.ScoutingStrategies;
using Bot.Managers.ScoutManagement.ScoutingTasks;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot;

public class FullMapScoutingStrategy : IScoutingStrategy {
    private bool _isInitialized = false;

    private List<ScoutingTask> _ongoingTasks = new List<ScoutingTask>();
    private HashSet<Region> _scoutedRegions = new HashSet<Region>();

    public IEnumerable<ScoutingTask> GetNextScoutingTasks() {
        if (!ExpandAnalyzer.IsInitialized || !RegionAnalyzer.IsInitialized) {
            yield break;
        }

        if (!_isInitialized) {
            _isInitialized = true;
            var mainBaseRegion = ExpandAnalyzer.GetExpand(Alliance.Self, ExpandType.Main).GetRegion();
            yield return AddTask(new RegionScoutingTask(mainBaseRegion.Center, priority: 0, maxScouts: 999));

            foreach (var neighbor in mainBaseRegion.Neighbors.Where(neighbor => !_scoutedRegions.Contains(neighbor.Region))) {
                yield return AddTask(new RegionScoutingTask(neighbor.Region.Center, priority: 0, maxScouts: 999));
            }

            yield break;
        }

        foreach (var scoutingTask in _ongoingTasks.Where(scoutingTask => scoutingTask.IsComplete()).ToList()) {
            _ongoingTasks.Remove(scoutingTask);

            var region = scoutingTask.ScoutLocation.GetRegion();
            foreach (var neighbor in region.Neighbors.Where(neighbor => !_scoutedRegions.Contains(neighbor.Region))) {
                yield return AddTask(new RegionScoutingTask(neighbor.Region.Center, priority: 0, maxScouts: 999));
            }
        }
    }

    private ScoutingTask AddTask(ScoutingTask scoutingTask) {
        _ongoingTasks.Add(scoutingTask);
        _scoutedRegions.Add(scoutingTask.ScoutLocation.GetRegion());

        return scoutingTask;
    }
}
