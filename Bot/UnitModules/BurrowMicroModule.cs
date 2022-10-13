using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;

namespace Bot.UnitModules;

// TODO GD Add a Flee method on Unit, and make it so Roaches can flee by foot or by burrowing?
public class BurrowMicroModule: UnitModule {
    public const string Tag = "BurrowMicroModule";

    public const double BurrowDownThreshold = 0.5;
    private const double BurrowUpThreshold = 0.6;

    private readonly Unit _unit;

    public static void Install(Unit unit) {
        if (PreInstallCheck(Tag, unit)) {
            unit.Modules.Add(Tag, new BurrowMicroModule(unit));
        }
    }

    private BurrowMicroModule(Unit unit) {
        _unit = unit;
    }

    protected override void DoExecute() {
        if (!Controller.ResearchedUpgrades.Contains(Upgrades.Burrow)) {
            return;
        }

        // Detection, abort
        if (DetectionTracker.IsDetected(_unit)) {
            if (_unit.IsBurrowed) {
                _unit.UseAbility(Abilities.BurrowRoachUp);
            }

            return;
        }

        if (!_unit.IsBurrowed && _unit.Integrity <= BurrowDownThreshold) {
            // Try to burrow down
            if (!GetCollidingUnits(checkUnderground: true).Any()) {
                _unit.UseAbility(Abilities.BurrowRoachDown);
            }
        }
        else if (_unit.IsBurrowed) {
            // Try to burrow up / flee while healing
            if (_unit.Integrity >= BurrowUpThreshold) {
                HandleBurrowUp();
            }
            else if (Controller.ResearchedUpgrades.Contains(Upgrades.TunnelingClaws)) {
                DigToSafety();
            }
        }
    }

    private void HandleBurrowUp() {
        var collidingUnits = GetCollidingUnits(checkUnderground: false);
        if (collidingUnits.Count > 0) {
            MoveAwayFrom(collidingUnits.GetCenter());
        }
        else {
            _unit.UseAbility(Abilities.BurrowRoachUp);
        }
    }

    private List<Unit> GetCollidingUnits(bool checkUnderground) {
        return UnitsTracker.UnitsByTag.Values
            .Where(otherUnit => !Units.Buildings.Contains(otherUnit.UnitType)) // We don't care about the buildings
            .Where(otherUnit => !otherUnit.IsFlying) // Flying units can't collide with ground units
            .Where(otherUnit => otherUnit.IsBurrowed == checkUnderground)
            .Where(otherUnit => otherUnit != _unit)
            .Where(otherUnit => otherUnit.DistanceTo(_unit) < (otherUnit.Radius + _unit.Radius) * 0.90) // Some overlap is possible
            .ToList();
    }

    private void DigToSafety() {
        if (!MoveOutOfEnemyRange() && !PrepareForUnburrowing()) {
            // TODO GD This prevents anyone from controlling this burrowed unit, which is good and bad
            _unit.Stop();
        }
    }

    private bool MoveOutOfEnemyRange() {
        var enemiesCanHitUs = Controller.GetUnits(UnitsTracker.EnemyUnits, Units.Military).Any(enemy => enemy.IsInRangeOf(_unit));
        if (!enemiesCanHitUs) {
            return false;
        }

        // Run to safety
        var safestRegion = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.TownHalls)
            .Select(townHall => townHall.GetRegion())
            .MinBy(RegionTracker.GetDangerLevel);

        if (safestRegion == null) {
            safestRegion = RegionAnalyzer.Regions.MinBy(RegionTracker.GetDangerLevel);
        }

        _unit.Move(safestRegion!.Center);

        return true;
    }

    private bool PrepareForUnburrowing() {
        // Pre-emptively ensure not colliding
        var collidingUnits = GetCollidingUnits(checkUnderground: false);
        if (collidingUnits.Count <= 0) {
            return false;
        }

        MoveAwayFrom(collidingUnits.GetCenter()); // We might want to consider enemy units as to not wiggle between colliding units and enemies?

        return true;
    }

    private void MoveAwayFrom(Vector3 position) {
        // TODO Check if you're creating a bottleneck
        _unit.Move(_unit.Position.TranslateAwayFrom(position, 1f), precision: 0.25f);
    }
}
