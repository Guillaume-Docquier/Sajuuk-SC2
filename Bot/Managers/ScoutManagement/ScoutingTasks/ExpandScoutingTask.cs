using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.Managers.ScoutManagement.ScoutingTasks;

public class ExpandScoutingTask : ScoutingTask {
    private readonly bool _waitForExpand;

    private bool _isCancelled = false;

    public ExpandScoutingTask(Vector3 scoutLocation, int priority, int maxScouts, bool waitForExpand = false)
        : base(scoutLocation, priority, maxScouts) {
        _waitForExpand = waitForExpand;
    }

    public override bool IsComplete() {
        if (_isCancelled) {
            return true;
        }

        if (!_waitForExpand && VisibilityTracker.IsVisible(ScoutLocation)) {
            return true;
        }

        return Controller.GetUnits(UnitsTracker.EnemyUnits, Units.TownHalls)
            .Where(enemyTownHall => enemyTownHall.IsVisible)
            .Any(enemyTownHall => enemyTownHall.HorizontalDistanceTo(ScoutLocation) <= enemyTownHall.Radius);
    }

    public override void Cancel() {
        _isCancelled = true;
    }

    public override void Execute(HashSet<Unit> scouts) {
        foreach (var scout in scouts) {
            var positionInSight = ScoutLocation.TranslateTowards(scout.Position, scout.UnitTypeData.SightRange + KnowledgeBase.GetBuildingRadius(Units.Hatchery));
            if (!scout.IsFlying) {
                positionInSight = positionInSight.ClosestWalkable();
            }

            scout.Move(positionInSight);
        }
    }
}
