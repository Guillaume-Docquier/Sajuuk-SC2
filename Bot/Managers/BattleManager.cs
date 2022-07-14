using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.UnitModules;

namespace Bot.Managers;

public class BattleManager: IManager {
    private Vector3 _target;
    private readonly List<Unit> _army = new List<Unit>();
    private bool _startAttacking = false;

    public BattleManager() {

    }

    public void Assign(Vector3 target) {
        _target = target;
    }

    public void Assign(List<Unit> soldiers) {
        soldiers.ForEach(unit => {
            unit.AddDeathWatcher(this);

            if (unit.UnitType == Units.Roach) {
                BurrowMicroModule.Install(unit);
            }
        });

        _army.AddRange(soldiers);

        if (_army.Sum(unit => unit.Supply) >= 20) {
            _startAttacking = true;
        }
    }

    public void OnFrame() {
        if (_startAttacking) {
            _army.ForEach(unit => unit.AttackMove(_target));
        }
    }

    public void ReportUnitDeath(Unit deadUnit) {
        _army.Remove(deadUnit);
    }
}
