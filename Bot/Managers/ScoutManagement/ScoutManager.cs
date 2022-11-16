using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
using Bot.Managers.ScoutManagement.ScoutingStrategies;
using Bot.Managers.ScoutManagement.ScoutingSupervision;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Managers.ScoutManagement;

public partial class ScoutManager : Manager {
    public override IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();

    protected override IAssigner Assigner { get; }
    protected override IDispatcher Dispatcher { get; }
    protected override IReleaser Releaser { get; }

    private readonly IScoutingStrategy _scoutingStrategy;
    private readonly HashSet<ScoutSupervisor> _scoutSupervisors = new HashSet<ScoutSupervisor>();

    public ScoutManager() {
        Assigner = new ScoutManagerAssigner(this);
        Dispatcher = new ScoutManagerDispatcher(this);
        Releaser = new ScoutManagerReleaser(this);

        _scoutingStrategy = ScoutingStrategyFactory.CreateNew(Controller.EnemyRace);
    }

    protected override void AssignmentPhase() {
        Assign(Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Overlord).Where(unit => unit.Manager == null));

        // Add some condition to request a Drone / Zergling
    }

    protected override void DispatchPhase() {
        foreach (var scoutingTask in _scoutingStrategy.Execute()) {
            _scoutSupervisors.Add(new ScoutSupervisor(scoutingTask));
        }

        // TODO GD We might want to reassign as we go?
        Dispatch(ManagedUnits.Where(unit => unit.Supervisor == null));
    }

    protected override void ManagementPhase() {
        if (!RegionAnalyzer.IsInitialized) {
            return;
        }

        ClearCompletedTasks();

        // TODO GD Consider releasing units
        foreach (var scoutSupervisor in _scoutSupervisors) {
            scoutSupervisor.OnFrame();
        }

        var unitsToRecall = ManagedUnits.Where(unit => unit.Supervisor == null).ToHashSet();
        foreach (var unitThatIsAvoidingDanger in AvoidDanger(unitsToRecall)) {
            unitsToRecall.Remove(unitThatIsAvoidingDanger);
        }

        // TODO GD Change to most defended?
        var safestRegion = RegionAnalyzer.Regions
            .OrderBy(region => RegionTracker.GetForce(region, Alliance.Enemy))
            .ThenBy(region => region.Center.DistanceTo(MapAnalyzer.StartingLocation))
            .First();

        foreach (var unitToRecall in unitsToRecall.Where(unit => unit.DistanceTo(safestRegion.Center) > 5)) {
            unitToRecall.Move(safestRegion.Center);
        }
    }

    // TODO GD Make this reusable
    private static IEnumerable<Unit> AvoidDanger(HashSet<Unit> unitsToPreserve) {
        var unitsThatAreAvoidingDanger = new List<Unit>();

        var enemyUnits = UnitsTracker.EnemyUnits.Concat(UnitsTracker.EnemyGhostUnits.Values).ToList();
        var enemyAntiAir = enemyUnits.Where(enemyUnit => enemyUnit.CanHitAir).ToList();
        foreach (var unitToPreserve in unitsToPreserve) {
            var enemiesInVicinity = unitToPreserve.IsFlying
                ? enemyAntiAir.Where(enemy => unitToPreserve.IsInSightRangeOf(enemy, extraRange: -1.5f)).ToList()
                : enemyUnits.Where(enemy => unitToPreserve.IsInSightRangeOf(enemy, extraRange: -1.5f)).ToList();

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
}
