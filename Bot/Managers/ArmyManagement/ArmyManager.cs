using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.UnitModules;

namespace Bot.Managers.ArmyManagement;

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

        // TODO GD Tweak this, the cluster gets broken when it shouldn't
        _mainArmy = Clustering.DBSCAN(Army, 4, 2).MaxBy(army => army.GetForce());
        _mainArmy ??= Army;

        var startingStrategyName = _strategy.Name;
        var count = 0;
        while (_strategy.CanTransition()) {
            _strategy = _strategy.Transition();
            count++;

            if (count >= 6) {
                Logger.Error("ArmyManager: Looped more than 6 times trying to find a strategy");
                break;
            }
        }

        if (_strategy.Name != startingStrategyName) {
            Logger.Info("ArmyManager strategy changed from {0} to {1}", startingStrategyName, _strategy.Name);
        }

        _strategy.Execute();
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
