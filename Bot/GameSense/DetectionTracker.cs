using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;

namespace Bot.GameSense;

public static class DetectionTracker {
    private static bool _enemyHasDetectors = false;

    public static bool IsStealthEffective() {
        if (_enemyHasDetectors) {
            return false;
        }

        // We can't kill flying units for now, so we can cache this value
        _enemyHasDetectors = Controller.GetUnits(UnitsTracker.EnemyUnits, Units.MobileDetectors, includeCloaked: true).Any();

        return !_enemyHasDetectors;
    }

    public static bool IsDetected(Unit unit) {
        return IsDetected(new List<Unit> { unit });
    }

    public static bool IsDetected(IReadOnlyCollection<Unit> army) {
        return IsArmyScanned(army) || IsArmyInDetectorRange(army);
    }

    private static bool IsArmyScanned(IReadOnlyCollection<Unit> army) {
        var scanRadius = KnowledgeBase.GetEffectData(Effects.ScanSweep).Radius;

        return Controller.GetEffects(Effects.ScanSweep)
            .SelectMany(scanEffect => scanEffect.Pos.ToList())
            .Any(scan => army.Any(soldier => scan.ToVector2().DistanceTo(soldier.Position.ToVector2()) <= scanRadius));
    }

    private static bool IsArmyInDetectorRange(IReadOnlyCollection<Unit> army) {
        return Controller.GetUnits(UnitsTracker.EnemyUnits, Units.Detectors)
            .Any(detector => army.Any(soldier => soldier.DistanceTo(detector) <= detector.UnitTypeData.SightRange));
    }
}
