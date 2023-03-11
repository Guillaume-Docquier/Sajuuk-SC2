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

    private readonly List<ScoutingTask> _ongoingTasks = new List<ScoutingTask>();
    private readonly HashSet<Region> _scoutedRegions = new HashSet<Region>();

    public IEnumerable<ScoutingTask> GetNextScoutingTasks() {
        if (!ExpandAnalyzer.IsInitialized || !RegionAnalyzer.IsInitialized) {
            yield break;
        }

        if (!_isInitialized) {
            _isInitialized = true;
            var mainBaseRegion = ExpandAnalyzer.GetExpand(Alliance.Self, ExpandType.Main).GetRegion();
            yield return AddTask(new RegionScoutingTask(mainBaseRegion.Center));

            foreach (var neighborScoutingTask in CreateNeighboringScoutingTasks(mainBaseRegion)) {
                yield return AddTask(neighborScoutingTask);
            }

            yield break;
        }

        foreach (var scoutingTask in _ongoingTasks.Where(scoutingTask => scoutingTask.IsComplete()).ToList()) {
            _ongoingTasks.Remove(scoutingTask);

            var region = scoutingTask.ScoutLocation.GetRegion();
            foreach (var neighborScoutingTask in CreateNeighboringScoutingTasks(region)) {
                yield return AddTask(neighborScoutingTask);
            }
        }
    }

    private ScoutingTask AddTask(ScoutingTask scoutingTask) {
        _ongoingTasks.Add(scoutingTask);
        _scoutedRegions.Add(scoutingTask.ScoutLocation.GetRegion());

        return scoutingTask;
    }

    private IEnumerable<ScoutingTask> CreateNeighboringScoutingTasks(Region region) {
        var neighborsToScout = region.Neighbors
            .Where(neighbor => !neighbor.Region.IsObstructed)
            .Where(neighbor => !_scoutedRegions.Contains(neighbor.Region));

        foreach (var neighbor in neighborsToScout) {
            yield return new RegionScoutingTask(neighbor.Region.Center);
        }
    }
}
