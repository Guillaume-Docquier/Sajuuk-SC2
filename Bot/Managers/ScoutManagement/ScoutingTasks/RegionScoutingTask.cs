using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.MapKnowledge;

namespace Bot.Managers.ScoutManagement.ScoutingTasks;

public class RegionScoutingTask : ScoutingTask {
    private readonly Region _region;
    private readonly HashSet<Vector3> _cellsToExplore;

    public RegionScoutingTask(Vector3 scoutLocation) : base(scoutLocation) {
        _region = scoutLocation.GetRegion();
        _cellsToExplore = new HashSet<Vector3>(_region.Cells);
    }

    public override bool IsComplete() {
        return _cellsToExplore.Count <= 0;
    }

    public override void Execute(HashSet<Unit> scouts) {
        UpdateCellsToExplore();

        if (IsComplete()) {
            return;
        }

        var distanceToScouts = new Dictionary<Vector3, Dictionary<Unit, float>>();
        foreach (var cellToExplore in _cellsToExplore) {
            var distances = new Dictionary<Unit, float>();
            foreach (var scout in scouts) {
                distances[scout] = cellToExplore.HorizontalDistanceTo(scout);
            }

            distanceToScouts[cellToExplore] = distances;
        }

        foreach (var scout in scouts) {
            var cellToExplore = _cellsToExplore.MinBy(cell => {
                var cellDistances = distanceToScouts[cell];
                var myDistance = cellDistances[scout];
                var otherDistancesAverage = cellDistances.Average(kv => kv.Value);

                return myDistance + otherDistancesAverage;
            });

            scout.Move(cellToExplore);
        }
    }

    private void UpdateCellsToExplore() {
        foreach (var cellToExplore in _cellsToExplore.Where(VisibilityTracker.IsVisible).ToList()) {
            _cellsToExplore.Remove(cellToExplore);
        }
    }
}
