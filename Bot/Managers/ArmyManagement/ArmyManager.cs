using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.StateManagement;
using Bot.UnitModules;

namespace Bot.Managers.ArmyManagement;

public partial class ArmyManager: StateMachine, IManager {
    public readonly List<Unit> Army = new List<Unit>();
    private List<Unit> _mainArmy;

    private Vector3 _target;
    private float _blastRadius;
    private bool _canHuntTheEnemy = true;

    private float _strongestForce;

    public IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();
    public IEnumerable<Unit> ManagedUnits => Army;

    public ArmyManager() : base(new AttackState()) {}

    public void Assign(Vector3 target, float blastRadius, bool canHuntTheEnemy = true) {
        _target = target.WithWorldHeight();
        _blastRadius = blastRadius;
        _strongestForce = Army.GetForce();
        _canHuntTheEnemy = canHuntTheEnemy;
        TransitionTo(new AttackState());
    }

    public void Assign(List<Unit> soldiers) {
        soldiers.ForEach(unit => {
            unit.AddDeathWatcher(this);

            if (unit.UnitType is Units.Roach or Units.RoachBurrowed) {
                BurrowMicroModule.Install(unit);
            }

            AttackPriorityModule.Install(unit);
        });

        // TODO GD Use a targeting module
        Army.AddRange(soldiers);
    }

    public void OnFrame() {
        if (Army.Count <= 0) {
            return;
        }

        _mainArmy = Clustering.DBSCAN(Army, 4, 2).clusters.MaxBy(army => army.GetForce());
        _mainArmy ??= Army;

        State.OnFrame();
    }

    public void Release(Unit unit) {
        if (Army.Contains(unit)) {
            Army.Remove(unit);
            unit.RemoveDeathWatcher(this);
            UnitModule.Uninstall<BurrowMicroModule>(unit);
            UnitModule.Uninstall<AttackPriorityModule>(unit);
        }
    }

    public void Retire() {
        Army.ForEach(soldier => UnitModule.Uninstall<BurrowMicroModule>(soldier));
    }

    public void ReportUnitDeath(Unit deadUnit) {
        Army.Remove(deadUnit);
    }
}
