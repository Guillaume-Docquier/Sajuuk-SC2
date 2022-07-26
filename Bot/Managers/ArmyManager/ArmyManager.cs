using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;
using Bot.UnitModules;

namespace Bot.Managers;

public partial class ArmyManager: IManager {
    public readonly List<Unit> Army = new List<Unit>();
    private List<Unit> _mainArmy;

    private Vector3 _target;
    private float _blastRadius;
    private bool _canHuntTheEnemy = true;

    private float _strongestForce;

    private IStrategy _strategy;

    public IEnumerable<BuildOrders.BuildStep> BuildStepRequests => Enumerable.Empty<BuildOrders.BuildStep>();

    public void Assign(Vector3 target, float blastRadius, bool canHuntTheEnemy = true) {
        _target = target.WithWorldHeight();
        _blastRadius = blastRadius;
        _strongestForce = Army.GetForce();
        _canHuntTheEnemy = canHuntTheEnemy;
        _strategy = new AttackStrategy(this);
    }

    public void Assign(List<Unit> soldiers) {
        soldiers.ForEach(unit => {
            unit.AddDeathWatcher(this);

            if (unit.UnitType == Units.Roach) {
                BurrowMicroModule.Install(unit);
            }
        });

        // TODO GD Use a targeting module
        Army.AddRange(soldiers);
    }

    public void OnFrame() {
        if (Army.Count <= 0) {
            return;
        }

        _mainArmy = Clustering.DBSCAN(Army, 3, 3).MaxBy(army => army.GetForce());
        _mainArmy ??= Army;

        while (_strategy.CanTransition()) {
            _strategy = _strategy.Transition();
        }

        _strategy.Execute();
    }

    public void Retire() {
        Army.ForEach(soldier => UnitModule.Uninstall<BurrowMicroModule>(soldier));
    }

    public void ReportUnitDeath(Unit deadUnit) {
        Army.Remove(deadUnit);
    }
}
