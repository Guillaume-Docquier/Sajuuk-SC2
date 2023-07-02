using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameData;
using Sajuuk.GameSense;

namespace Sajuuk.Managers.ScoutManagement.ScoutingTasks;

public class ExpandScoutingTask : ScoutingTask {
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly KnowledgeBase _knowledgeBase;

    private readonly bool _waitForExpand;
    private readonly float _expandRadius;

    private bool _isCancelled = false;

    // TODO GD Create a factory to avoid passing services all the time
    public ExpandScoutingTask(
        IVisibilityTracker visibilityTracker,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        KnowledgeBase knowledgeBase,
        Vector2 scoutLocation,
        int priority,
        int maxScouts,
        bool waitForExpand
    ) : base(scoutLocation, priority, maxScouts) {
        _visibilityTracker = visibilityTracker;
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _knowledgeBase = knowledgeBase;

        _waitForExpand = waitForExpand;

        // We make the expand radius 0 if we don't wait for confirmation just to
        // gain a bit of extra vision since we'll be on our way right away
        _expandRadius = _waitForExpand
            ? _knowledgeBase.GetBuildingRadius(Units.Hatchery)
            : 0;
    }

    public override bool IsComplete() {
        if (_isCancelled) {
            return true;
        }

        if (!_waitForExpand && _visibilityTracker.IsVisible(ScoutLocation)) {
            return true;
        }

        return _unitsTracker.GetUnits(_unitsTracker.EnemyUnits, Units.TownHalls)
            .Where(enemyTownHall => enemyTownHall.IsVisible)
            .Any(enemyTownHall => enemyTownHall.DistanceTo(ScoutLocation) <= enemyTownHall.Radius);
    }

    public override void Cancel() {
        _isCancelled = true;
    }

    public override void Execute(HashSet<Unit> scouts) {
        foreach (var scout in scouts) {
            var positionInSight = ScoutLocation.TranslateTowards(scout.Position.ToVector2(), scout.UnitTypeData.SightRange + _expandRadius);
            if (!scout.IsFlying) {
                positionInSight = _terrainTracker.GetClosestWalkable(positionInSight);
            }

            scout.Move(positionInSight);
        }
    }
}
