﻿using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.GameSense;
using Sajuuk.Managers.ScoutManagement.ScoutingTasks;

namespace Sajuuk.Managers.ScoutManagement.ScoutingSupervision;

public class ScoutSupervisor : Supervisor {
    private readonly IUnitsTracker _unitsTracker;

    public readonly ScoutingTask ScoutingTask;

    public override IEnumerable<IFulfillableBuildRequest> BuildRequests => Enumerable.Empty<IFulfillableBuildRequest>();

    public ScoutSupervisor(IUnitsTracker unitsTracker, ScoutingTask scoutingTask) {
        _unitsTracker = unitsTracker;

        ScoutingTask = scoutingTask;
    }

    protected override void Supervise() {
        var scoutsThatCanWork = new HashSet<Unit>(SupervisedUnits);

        var enemyUnits = _unitsTracker.EnemyUnits.Concat(_unitsTracker.EnemyGhostUnits.Values).ToList();
        var antiAirEnemies = enemyUnits.Where(enemyUnit => enemyUnit.CanHitAir).ToList();
        var antiGroundEnemies = enemyUnits.Where(enemyUnit => enemyUnit.CanHitGround).ToList();
        foreach (var unitToPreserve in SupervisedUnits) {
            var enemiesInVicinity = unitToPreserve.IsFlying
                ? antiAirEnemies.Where(enemy => unitToPreserve.IsInSightRangeOf(enemy)).ToList()
                : antiGroundEnemies.Where(enemy => unitToPreserve.IsInSightRangeOf(enemy)).ToList();

            if (enemiesInVicinity.Count > 0) {
                var fleeVector = ComputeFleeUnitVector(unitToPreserve, enemiesInVicinity);
                unitToPreserve.MoveInDirection(3 * fleeVector);
                scoutsThatCanWork.Remove(unitToPreserve);
            }
        }

        ScoutingTask.Execute(scoutsThatCanWork);
    }

    public override void Retire() {
        ScoutingTask.Cancel();
        foreach (var supervisedUnit in SupervisedUnits) {
            Release(supervisedUnit);
        }
    }

    private static Vector2 ComputeFleeUnitVector(Unit unitToPreserve, IEnumerable<Unit> enemyUnitsToAvoid) {
        var avoidanceVectors = enemyUnitsToAvoid.Select(enemy => Vector2.Divide(enemy.Position.DirectionTo(unitToPreserve.Position).ToVector2(), (float)enemy.DistanceTo(unitToPreserve)));

        var fleeX = 0f;
        var fleeY = 0f;
        foreach (var avoidanceVector in avoidanceVectors) {
            fleeX += avoidanceVector.X;
            fleeY += avoidanceVector.Y;
        }

        return Vector2.Normalize(new Vector2(fleeX, fleeY));
    }

    // TODO GD Rework assigner/dispatcher/releaser. It's not very helpful
    protected override IAssigner Assigner { get; } = new DummyAssigner();
    protected override IReleaser Releaser { get; } = new DummyReleaser();
    private class DummyAssigner : IAssigner { public void Assign(Unit unit) {} }
    private class DummyReleaser : IReleaser { public void Release(Unit unit) {} }
}
