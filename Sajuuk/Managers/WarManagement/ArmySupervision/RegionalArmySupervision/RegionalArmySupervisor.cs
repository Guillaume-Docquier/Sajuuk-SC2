﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sajuuk.ExtensionMethods;
using Sajuuk.Algorithms;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameSense;
using Sajuuk.Managers.WarManagement.ArmySupervision.UnitsControl;
using Sajuuk.MapAnalysis.RegionAnalysis;
using Sajuuk.StateManagement;

namespace Sajuuk.Managers.WarManagement.ArmySupervision.RegionalArmySupervision;

public class RegionalArmySupervisor : Supervisor {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;

    private const bool Debug = false;

    private readonly IUnitsControl _offensiveUnitsController;
    private readonly IUnitsControl _defensiveUnitsController;
    private readonly IRegion _targetRegion;
    private readonly IClustering _clustering;
    private readonly StateMachine<RegionalArmySupervisor, RegionalArmySupervisionState> _stateMachine;

    public override IEnumerable<IFulfillableBuildRequest> BuildRequests => Enumerable.Empty<IFulfillableBuildRequest>();

    public RegionalArmySupervisor(
        IUnitsTracker unitsTracker,
        IGraphicalDebugger graphicalDebugger,
        IUnitsControlFactory unitsControlFactory,
        IRegionalArmySupervisorStateFactory regionalArmySupervisorStateFactory,
        IClustering clustering,
        IRegion targetRegion
    ) {
        _unitsTracker = unitsTracker;
        _graphicalDebugger = graphicalDebugger;
        _clustering = clustering;

        _targetRegion = targetRegion;

        _offensiveUnitsController = unitsControlFactory.CreateOffensiveUnitsControl();
        _defensiveUnitsController = unitsControlFactory.CreateDefensiveUnitsControl();

        Releaser = new RegionalArmySupervisorReleaser(this);
        _stateMachine = new StateMachine<RegionalArmySupervisor, RegionalArmySupervisionState>(this, regionalArmySupervisorStateFactory.CreateApproachState());
    }

    protected override void Supervise() {
        if (!SupervisedUnits.Any()) {
            return;
        }

        _stateMachine.State.EnemyArmy = GetEnemyArmy(_targetRegion).ToList();
        _stateMachine.State.SupervisedUnits = SupervisedUnits;
        _stateMachine.State.TargetRegion = _targetRegion;
        // TODO GD If states were not re-created all the time, we would be able to provide ...
        // TODO GD ... IUnitsControls at creation time, rather than anonymously every frame
        _stateMachine.State.OffensiveUnitsController = _offensiveUnitsController;
        _stateMachine.State.DefensiveUnitsController = _defensiveUnitsController;

        _stateMachine.OnFrame();

        DebugUnits();
    }

    /// <summary>
    /// Gets the enemy units that need to be defeated.
    /// This includes all units that are in a cluster where one member is in the target region.
    /// </summary>
    /// <param name="targetRegion">The target region.</param>
    /// <returns>The enemy units to defeat</returns>
    private IEnumerable<Unit> GetEnemyArmy(IRegion targetRegion) {
        var enemies = _unitsTracker.EnemyUnits
            .Concat(_unitsTracker.EnemyGhostUnits.Values)
            .Where(enemy => !enemy.IsFlying) // TODO GD Bad bad hardcode
            .ToList();

        var clusteringResult = _clustering.DBSCAN(enemies, 2, 3);

        return clusteringResult.clusters
            // TODO GD We should also consider any unit in range of the region (like siege tanks up a ramp)
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
            _graphicalDebugger.AddText($"{stateSymbol} {_targetRegion.Id:00}", worldPos: supervisedUnit.Position.ToPoint(yOffset: -0.17f), color: Colors.Yellow);
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

    public override string ToString() {
        return $"RegionalArmySupervisor[{_targetRegion.Id}]";
    }

    // TODO GD Rework assigner/dispatcher/releaser. It's not very helpful
    protected override IAssigner Assigner { get; } = new DummyAssigner();
    protected override IReleaser Releaser { get; }

    private class DummyAssigner : IAssigner { public void Assign(Unit unit) {} }
    private class RegionalArmySupervisorReleaser : Releaser<RegionalArmySupervisor> {
        public RegionalArmySupervisorReleaser(RegionalArmySupervisor client) : base(client) {}
        public override void Release(Unit unit) {
            Client._stateMachine.State.Release(unit);
        }
    }
}
