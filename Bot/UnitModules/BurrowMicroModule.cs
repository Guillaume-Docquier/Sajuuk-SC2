using System.Linq;
using Bot.GameData;

namespace Bot.UnitModules;

// TODO GD Only works on roaches for now
public class BurrowMicroModule: IUnitModule {
    public const string Tag = "burrow-micro-module";

    private const double BurrowDownThreshold = 0.5;
    private const double BurrowUpThreshold = 0.6;

    private readonly Unit _unit;

    public static void Install(Unit unit) {
        unit.Modules.Add(Tag, new BurrowMicroModule(unit));
    }

    public static BurrowMicroModule Uninstall(Unit worker) {
        var module = GetFrom(worker);
        worker.Modules.Remove(Tag);

        return module;
    }

    public static BurrowMicroModule GetFrom(Unit worker) {
        if (worker.Modules.TryGetValue(Tag, out var module)) {
            return module as BurrowMicroModule;
        }

        return null;
    }

    private BurrowMicroModule(Unit unit) {
        _unit = unit;
    }

    public void Execute() {
        if (!Controller.ResearchedUpgrades.Contains(Upgrades.Burrow)) {
            return;
        }

        // Try to burrow down
        if (_unit.Integrity <= BurrowDownThreshold && !_unit.RawUnitData.IsBurrowed) {
            if (ThereIsAUnitAtYourLocation(checkUnderground: true)) {
                MoveAwayFromTheEnemy(GetClosestEnemyUnit());
            }
            else {
                _unit.UseAbility(Abilities.BurrowRoachDown);
            }
        }

        // Try to burrow up / flee while healing
        if (_unit.RawUnitData.IsBurrowed) {
            var thereIsAUnitOverMe = ThereIsAUnitAtYourLocation(checkUnderground: false);

            if (!thereIsAUnitOverMe && _unit.Integrity >= BurrowUpThreshold) {
                _unit.UseAbility(Abilities.BurrowRoachUp);
            }
            else if (Controller.ResearchedUpgrades.Contains(Upgrades.TunnelingClaws)) {
                var closestEnemyUnit = GetClosestEnemyUnit();

                if (thereIsAUnitOverMe) {
                    MoveAwayFromTheEnemy(closestEnemyUnit);
                }
                else if (closestEnemyUnit != null && _unit.DistanceTo(closestEnemyUnit) <= 5) {
                    MoveAwayFromTheEnemy(closestEnemyUnit);
                }
            }
        }
    }

    private bool ThereIsAUnitAtYourLocation(bool checkUnderground) {
       return Controller.UnitsByTag.Values
            .Where(otherUnit => otherUnit != _unit)
            .Where(otherUnit => otherUnit.RawUnitData.IsBurrowed == checkUnderground)
            .Any(otherUnit => otherUnit.DistanceTo(_unit) <= otherUnit.Radius + _unit.Radius);
    }

    private void MoveAwayFromTheEnemy(Unit closestEnemyUnit) {
        // TODO Check if you're creating a bottleneck
        if (closestEnemyUnit != null) {
            _unit.Move(_unit.Position.TranslateAwayFrom(closestEnemyUnit.Position, 1f));
        }
    }

    // TODO GD Cache this
    private Unit GetClosestEnemyUnit() {
        return Controller.EnemyUnits.OrderBy(enemyUnit => _unit.DistanceTo(enemyUnit)).FirstOrDefault();
    }
}
