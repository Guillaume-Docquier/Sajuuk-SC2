using System;
using System.Collections.Generic;
using System.Linq;
using Bot.Algorithms;
using Bot.Builds;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl;
using Bot.MapKnowledge;
using Bot.StateManagement;

namespace Bot.Managers.WarManagement.ArmySupervision.RegionalArmySupervision;

public class RegionalArmySupervisor : Supervisor {
    private const bool Debug = false;

    private readonly IUnitsControl _unitsController = new UnitsController();
    private readonly IRegion _targetRegion;
    private readonly StateMachine<RegionalArmySupervisor, RegionalArmySupervisionState> _stateMachine;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();

    public RegionalArmySupervisor(IRegion targetRegion) {
        _targetRegion = targetRegion;
        _stateMachine = new StateMachine<RegionalArmySupervisor, RegionalArmySupervisionState>(this, new ApproachState());
    }

    protected override void Supervise() {
        if (!SupervisedUnits.Any()) {
            return;
        }

        _stateMachine.State.EnemyArmy = GetEnemyArmy(_targetRegion).ToList();
        _stateMachine.State.SupervisedUnits = SupervisedUnits;
        _stateMachine.State.TargetRegion = _targetRegion;
        _stateMachine.State.UnitsController = _unitsController;

        _stateMachine.OnFrame();

        DebugUnits();
    }

    /// <summary>
    /// Gets the enemy units that need to be defeated.
    /// This includes all units that are in a cluster where one member is in the target region.
    /// </summary>
    /// <param name="targetRegion">The target region.</param>
    /// <returns>The enemy units to defeat</returns>
    private static IEnumerable<Unit> GetEnemyArmy(IRegion targetRegion) {
        var enemies = UnitsTracker.EnemyUnits
            .Concat(UnitsTracker.EnemyGhostUnits.Values)
            .Where(enemy => !enemy.IsFlying) // TODO GD Bad bad hardcode
            .ToList();

        var clusteringResult = Clustering.DBSCAN(enemies, 2, 3);

        return clusteringResult.clusters
            .Where(cluster => cluster.Any(unit => unit.GetRegion() == targetRegion))
            .SelectMany(cluster => cluster)
            .Concat(clusteringResult.noise.Where(unit => unit.GetRegion() == targetRegion));
    }

    private void DebugUnits() {
        if (!Debug) {
            return;
        }

        var stateSymbol = _stateMachine.State switch
        {
            ApproachState => "MOVE",
            EngageState => "ATK",
            DisengageState => "RUN",
            _ => throw new ArgumentOutOfRangeException()
        };

        foreach (var supervisedUnit in SupervisedUnits) {
            Program.GraphicalDebugger.AddText($"{stateSymbol} {_targetRegion.Id:00}", worldPos: supervisedUnit.Position.ToPoint(yOffset: -0.17f), color: Colors.Yellow);
        }
    }

    public override void Retire() {
        foreach (var supervisedUnit in SupervisedUnits) {
            Release(supervisedUnit);
        }
    }

    public IEnumerable<Unit> GetReleasableUnits() {
        return _stateMachine.State.GetReleasableUnits();
    }

    // TODO GD Rework assigner/releaser. It's not helpful at all
    protected override IAssigner Assigner { get; } = new DummyAssigner();
    protected override IReleaser Releaser { get; } = new DummyReleaser();

    private class DummyAssigner : IAssigner { public void Assign(Unit unit) {} }
    private class DummyReleaser : IReleaser { public void Release(Unit unit) {} }
}
