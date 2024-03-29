﻿using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.Algorithms;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.GameSense;
using Sajuuk.StateManagement;

namespace Sajuuk.Managers.WarManagement.ArmySupervision;

public partial class ArmySupervisor : Supervisor {
    private readonly IArmySupervisorStateFactory _armySupervisorStateFactory;
    private readonly IClustering _clustering;
    private readonly IUnitEvaluator _unitEvaluator;

    private readonly StateMachine<ArmySupervisor> _stateMachine;

    public readonly List<Unit> Army = new List<Unit>();
    private List<Unit> _mainArmy;

    private Vector2 _target;
    private float _blastRadius;
    private bool _canHuntTheEnemy = true;

    private float _strongestForce;
    private bool CanHitAirUnits => SupervisedUnits.Any(supervisedUnit => supervisedUnit.CanHitAir);
    private bool CanFly => SupervisedUnits.Any(supervisedUnit => supervisedUnit.IsFlying);

    public override IEnumerable<IFulfillableBuildRequest> BuildRequests => Enumerable.Empty<IFulfillableBuildRequest>();

    protected override IAssigner Assigner { get; }
    protected override IReleaser Releaser { get; }

    public ArmySupervisor(
        IArmySupervisorStateFactory armySupervisorStateFactory,
        IClustering clustering,
        IUnitEvaluator unitEvaluator
    ) {
        _armySupervisorStateFactory = armySupervisorStateFactory;
        _clustering = clustering;
        _unitEvaluator = unitEvaluator;

        Assigner = new ArmySupervisorAssigner(this);
        Releaser = new ArmySupervisorReleaser(this);

        _stateMachine = new StateMachine<ArmySupervisor>(this, _armySupervisorStateFactory.CreateAttackState());
    }

    public override string ToString() {
        return "ArmySupervisor";
    }

    protected override void Supervise() {
        if (Army.Count <= 0) {
            return;
        }

        _mainArmy = _clustering.DBSCAN(Army, 4, 2).clusters.MaxBy(army => _unitEvaluator.EvaluateForce(army));
        _mainArmy ??= Army; // TODO GD This is bad, let the states do what they want

        _stateMachine.OnFrame();
    }

    public void AssignTarget(Vector2 target, float blastRadius, bool canHuntTheEnemy = true) {
        if (_target != target) {
            _target = target;
            _stateMachine.TransitionTo(_armySupervisorStateFactory.CreateAttackState());
        }

        _blastRadius = blastRadius;
        _strongestForce = _unitEvaluator.EvaluateForce(Army);
        _canHuntTheEnemy = canHuntTheEnemy;
    }

    public override void Retire() {
        foreach (var unit in Army.ToList()) {
            Release(unit);
        }

        // Reset the state to have a clean slate once we're re-hired
        // Maybe our Manager should just dispose of us instead?
        _stateMachine.TransitionTo(_armySupervisorStateFactory.CreateAttackState());
    }
}
