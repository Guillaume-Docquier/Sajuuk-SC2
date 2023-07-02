using System.Collections.Generic;
using System.Linq;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameData;

namespace Sajuuk.GameSense;

public class DetectionTracker : IDetectionTracker {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IController _controller;
    private readonly KnowledgeBase _knowledgeBase;

    private static bool _enemyHasDetectors = false;

    public DetectionTracker(
        IUnitsTracker unitsTracker,
        IController controller,
        KnowledgeBase knowledgeBase
    ) {
        _unitsTracker = unitsTracker;
        _controller = controller;
        _knowledgeBase = knowledgeBase;
    }

    public bool IsStealthEffective() {
        if (_enemyHasDetectors) {
            return false;
        }

        // We can't kill flying units for now, so we can cache this value
        _enemyHasDetectors = _unitsTracker.GetUnits(_unitsTracker.EnemyUnits, Units.MobileDetectors, includeCloaked: true).Any();

        return !_enemyHasDetectors;
    }

    public bool IsDetected(Unit unit) {
        return IsDetected(new List<Unit> { unit });
    }

    public bool IsDetected(IReadOnlyCollection<Unit> army) {
        return IsArmyScanned(army) || IsArmyInDetectorRange(army);
    }

    private bool IsArmyScanned(IReadOnlyCollection<Unit> army) {
        var scanRadius = _knowledgeBase.GetEffectData(Effects.ScanSweep).Radius;

        return _controller.GetEffects(Effects.ScanSweep)
            .SelectMany(scanEffect => scanEffect.Pos.ToList())
            .Any(scan => army.Any(soldier => scan.ToVector2().DistanceTo(soldier.Position.ToVector2()) <= scanRadius));
    }

    private bool IsArmyInDetectorRange(IReadOnlyCollection<Unit> army) {
        return _unitsTracker.GetUnits(_unitsTracker.EnemyUnits, Units.Detectors)
            .Any(detector => army.Any(soldier => soldier.DistanceTo(detector) <= detector.UnitTypeData.SightRange));
    }
}
