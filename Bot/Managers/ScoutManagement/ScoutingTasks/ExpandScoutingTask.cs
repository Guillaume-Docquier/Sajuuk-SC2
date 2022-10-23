using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.Managers.ScoutManagement.ScoutingTasks;

public class ExpandScoutingTask : ScoutingTask {
    private bool _isCancelled = false;

    public ExpandScoutingTask(Vector3 scoutLocation, int priority, int maxScouts)
        : base(scoutLocation, priority, maxScouts) {
    }

    public override bool IsComplete() {
        if (_isCancelled) {
            return true;
        }

        if (VisibilityTracker.IsVisible(ScoutLocation)) {
            return true;
        }

        // TODO GD Maybe add a target frame to wait for? Or do we handle it via a follow up?
        return Controller.GetUnits(UnitsTracker.EnemyUnits, Units.TownHalls)
            .Where(unit => unit.IsVisible)
            .Any(unit => unit.HorizontalDistanceTo(ScoutLocation) <= unit.Radius);
    }

    public override void Cancel() {
        _isCancelled = true;
    }

    public override void Execute(HashSet<Unit> scouts) {
        foreach (var scout in scouts) {
            scout.Move(ScoutLocation);
        }
    }
}
