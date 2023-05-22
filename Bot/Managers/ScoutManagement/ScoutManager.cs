using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers.ScoutManagement.ScoutingStrategies;
using Bot.Managers.ScoutManagement.ScoutingSupervision;

namespace Bot.Managers.ScoutManagement;

public partial class ScoutManager : Manager {
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IScoutSupervisorFactory _scoutSupervisorFactory;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();

    protected override IAssigner Assigner { get; }
    protected override IDispatcher Dispatcher { get; }
    protected override IReleaser Releaser { get; }

    private readonly IScoutingStrategy _scoutingStrategy;
    private readonly HashSet<ScoutSupervisor> _scoutSupervisors = new HashSet<ScoutSupervisor>();

    public ScoutManager(
        IEnemyRaceTracker enemyRaceTracker,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IScoutSupervisorFactory scoutSupervisorFactory,
        IScoutingStrategyFactory scoutingStrategyFactory
    ) {
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _scoutSupervisorFactory = scoutSupervisorFactory;

        Assigner = new ScoutManagerAssigner(this);
        Dispatcher = new ScoutManagerDispatcher(this);
        Releaser = new ScoutManagerReleaser(this);

        _scoutingStrategy = scoutingStrategyFactory.CreateNew(enemyRaceTracker.EnemyRace);
    }

    protected override void RecruitmentPhase() {
        Assign(_unitsTracker.GetUnits(_unitsTracker.OwnedUnits, Units.Overlord).Where(unit => unit.Manager == null));

        // TODO GD Add some condition to request a Drone / Zergling?
    }

    protected override void DispatchPhase() {
        ClearCompletedTasks();

        foreach (var scoutingTask in _scoutingStrategy.GetNextScoutingTasks()) {
            _scoutSupervisors.Add(_scoutSupervisorFactory.CreateScoutSupervisor(scoutingTask));
        }

        // TODO GD Reassign as we go to avoid selecting a scout that is super far while a nearby one just finished work
        Dispatch(ManagedUnits.Where(unit => unit.Supervisor == null));
    }

    protected override void ManagementPhase() {
        // TODO GD Consider releasing units
        foreach (var scoutSupervisor in _scoutSupervisors) {
            scoutSupervisor.OnFrame();
        }

        var unitsToRecall = ManagedUnits.Where(unit => unit.Supervisor == null).ToHashSet();
        foreach (var unitThatIsAvoidingDanger in AvoidDanger(unitsToRecall)) {
            unitsToRecall.Remove(unitThatIsAvoidingDanger);
        }

        // TODO GD This can be improved but seems like a sensible default
        var recallPosition = _terrainTracker.StartingLocation;
        foreach (var unitToRecall in unitsToRecall.Where(unit => unit.DistanceTo(recallPosition) > 5)) {
            unitToRecall.Move(recallPosition);
        }
    }

    // TODO GD Make this reusable
    private IEnumerable<Unit> AvoidDanger(HashSet<Unit> unitsToPreserve) {
        var unitsThatAreAvoidingDanger = new List<Unit>();

        var enemyUnits = _unitsTracker.EnemyUnits.Concat(_unitsTracker.EnemyGhostUnits.Values).ToList();
        var antiAirEnemies = enemyUnits.Where(enemyUnit => enemyUnit.CanHitAir).ToList();
        var antiGroundEnemies = enemyUnits.Where(enemyUnit => enemyUnit.CanHitGround).ToList();
        foreach (var unitToPreserve in unitsToPreserve) {
            var enemiesInVicinity = unitToPreserve.IsFlying
                ? antiAirEnemies.Where(enemy => unitToPreserve.IsInSightRangeOf(enemy)).ToList()
                : antiGroundEnemies.Where(enemy => unitToPreserve.IsInSightRangeOf(enemy)).ToList();

            if (enemiesInVicinity.Count > 0) {
                var fleeVector = ComputeFleeUnitVector(unitToPreserve, enemiesInVicinity);
                unitToPreserve.MoveInDirection(3 * fleeVector);
                unitsThatAreAvoidingDanger.Add(unitToPreserve);
            }
        }

        return unitsThatAreAvoidingDanger;
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

    private void ClearCompletedTasks() {
        var completedTasks = _scoutSupervisors.Where(supervisor => supervisor.ScoutingTask.IsComplete());
        foreach (var scoutSupervisor in completedTasks) {
            scoutSupervisor.Retire();
            _scoutSupervisors.Remove(scoutSupervisor);
        }
    }

    protected override void OnManagedUnitDeath(Unit deadUnit) {
        // Our logic to protect scouts is bad right now
        // If we start losing scouts, abort all scouting and recall everyone
        foreach (var scoutSupervisor in _scoutSupervisors) {
            scoutSupervisor.Retire();
            _scoutSupervisors.Remove(scoutSupervisor);
        }
    }
}
