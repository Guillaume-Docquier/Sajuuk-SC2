using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.Managers.ScoutManagement.ScoutingTasks;

namespace Bot.Managers.ScoutManagement.ScoutSupervision;

public partial class ScoutSupervisor : Supervisor {
    public readonly ScoutingTask ScoutingTask;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();
    protected override IAssigner Assigner { get; }
    protected override IReleaser Releaser { get; }

    public ScoutSupervisor(ScoutingTask scoutingTask) {
        Assigner = new ScoutSupervisorAssigner(this);
        Releaser = new ScoutSupervisorReleaser(this);

        ScoutingTask = scoutingTask;
    }

    protected override void Supervise() {
        var scoutsThatCanWork = new HashSet<Unit>(SupervisedUnits);

        var enemyUnits = UnitsTracker.EnemyUnits.Concat(UnitsTracker.EnemyGhostUnits.Values).ToList();
        var enemyAntiAir = enemyUnits.Where(enemyUnit => enemyUnit.CanHitAir).ToList();
        foreach (var supervisedUnit in SupervisedUnits) {
            var enemiesInVicinity = supervisedUnit.IsFlying
                ? enemyAntiAir.Where(enemy => supervisedUnit.IsInSightRangeOf(enemy, extraRange: -1.5f)).ToList()
                : enemyUnits.Where(enemy => supervisedUnit.IsInSightRangeOf(enemy, extraRange: -1.5f)).ToList();

            if (enemiesInVicinity.Count > 0) {
                Controller.SetRealTime();
                var fleeVector = ComputeFleeUnitVector(supervisedUnit, enemiesInVicinity);
                supervisedUnit.MoveInDirection(3 * fleeVector);
                scoutsThatCanWork.Remove(supervisedUnit);
            }
        }

        ScoutingTask.Execute(scoutsThatCanWork);
    }

    public override void Retire() {
        foreach (var supervisedUnit in SupervisedUnits) {
            Release(supervisedUnit);
        }
    }

    private static Vector2 ComputeFleeUnitVector(Unit unitToPreserve, IEnumerable<Unit> enemyUnitsToAvoid) {
        var avoidanceVectors = enemyUnitsToAvoid.Select(enemy => Vector2.Divide(enemy.Position.DirectionTo(unitToPreserve.Position).ToVector2(), (float)enemy.HorizontalDistanceTo(unitToPreserve)));

        var fleeX = 0f;
        var fleeY = 0f;
        foreach (var avoidanceVector in avoidanceVectors) {
            fleeX += avoidanceVector.X;
            fleeY += avoidanceVector.Y;
        }

        return Vector2.Normalize(new Vector2(fleeX, fleeY));
    }
}
