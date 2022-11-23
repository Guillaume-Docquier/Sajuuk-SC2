using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.StateManagement;

namespace Bot.Managers.WarManagement.ArmySupervision;

public partial class ArmySupervisor: Supervisor {
    private readonly StateMachine<ArmySupervisor> _stateMachine;

    public readonly List<Unit> Army = new List<Unit>();
    private List<Unit> _mainArmy;

    private Vector2 _target;
    private float _blastRadius;
    private bool _canHuntTheEnemy = true;

    private float _strongestForce;
    private bool CanHitAirUnits => SupervisedUnits.Any(supervisedUnit => supervisedUnit.CanHitAir);
    private bool CanFly => SupervisedUnits.Any(supervisedUnit => supervisedUnit.IsFlying);

    public override IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();

    protected override IAssigner Assigner { get; }
    protected override IReleaser Releaser { get; }

    public ArmySupervisor() {
        Assigner = new ArmySupervisorAssigner(this);
        Releaser = new ArmySupervisorReleaser(this);

        _stateMachine = new StateMachine<ArmySupervisor>(this, new AttackState());
    }

    public override string ToString() {
        return "ArmySupervisor";
    }

    protected override void Supervise() {
        if (Army.Count <= 0) {
            return;
        }

        _mainArmy = Clustering.DBSCAN(Army, 4, 2).clusters.MaxBy(army => army.GetForce());
        _mainArmy ??= Army; // TODO GD This is bad, let the states do what they want

        _stateMachine.OnFrame();
    }

    public void AssignTarget(Vector2 target, float blastRadius, bool canHuntTheEnemy = true) {
        if (_target != target) {
            _target = target;
            _stateMachine.TransitionTo(new AttackState());
        }

        _blastRadius = blastRadius;
        _strongestForce = Army.GetForce();
        _canHuntTheEnemy = canHuntTheEnemy;
    }

    public override void Retire() {
        foreach (var unit in Army.ToList()) {
            Release(unit);
        }
    }
}
