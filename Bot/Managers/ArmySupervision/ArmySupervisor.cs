using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.StateManagement;

namespace Bot.Managers.ArmySupervision;

public partial class ArmySupervisor: Supervisor {
    private readonly StateMachine<ArmySupervisor> _stateMachine;

    public readonly List<Unit> Army = new List<Unit>();
    private List<Unit> _mainArmy;

    private Vector3 _target;
    private float _blastRadius;
    private bool _canHuntTheEnemy = true;

    private float _strongestForce;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();

    public static ArmySupervisor Create() {
        var supervisor = new ArmySupervisor();
        supervisor.Init();

        return supervisor;
    }

    private ArmySupervisor() {
        _stateMachine = new StateMachine<ArmySupervisor>(this, new AttackState());
    }

    protected override IAssigner CreateAssigner() {
        return new ArmySupervisorAssigner(this);
    }

    protected override IReleaser CreateReleaser() {
        return new ArmySupervisorReleaser(this);
    }

    protected override void Supervise() {
        if (Army.Count <= 0) {
            return;
        }

        _mainArmy = Clustering.DBSCAN(Army, 4, 2).clusters.MaxBy(army => army.GetForce());
        _mainArmy ??= Army; // TODO GD This is bad, let the states do what they want

        _stateMachine.OnFrame();
    }

    public void AssignTarget(Vector3 target, float blastRadius, bool canHuntTheEnemy = true) {
        _target = target.WithWorldHeight();
        _blastRadius = blastRadius;
        _strongestForce = Army.GetForce();
        _canHuntTheEnemy = canHuntTheEnemy;
        _stateMachine.TransitionTo(new AttackState());
    }

    public override void Retire() {
        Army.ForEach(Release);
    }

    public override string ToString() {
        return "ArmySupervisor";
    }
}
