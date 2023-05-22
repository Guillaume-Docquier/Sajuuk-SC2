using Bot.Algorithms;
using Bot.GameSense;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;

public class SneakAttackStateFactory : ISneakAttackStateFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IFrameClock _frameClock;
    private readonly IDetectionTracker _detectionTracker;
    private readonly IClustering _clustering;
    private readonly IUnitEvaluator _unitEvaluator;

    public SneakAttackStateFactory(
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IFrameClock frameClock,
        IDetectionTracker detectionTracker,
        IClustering clustering,
        IUnitEvaluator unitEvaluator
    ) {
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _frameClock = frameClock;
        _detectionTracker = detectionTracker;
        _clustering = clustering;
        _unitEvaluator = unitEvaluator;
    }

    public SneakAttack.ApproachState CreateApproachState() {
        return new SneakAttack.ApproachState(_detectionTracker, this);
    }

    public SneakAttack.EngageState CreateEngageState() {
        return new SneakAttack.EngageState(this);
    }

    public SneakAttack.InactiveState CreateInactiveState() {
        return new SneakAttack.InactiveState(_unitsTracker, _terrainTracker, _frameClock, _detectionTracker, _unitEvaluator, this);
    }

    public SneakAttack.SetupState CreateSetupState() {
        return new SneakAttack.SetupState(_unitsTracker, _terrainTracker, _detectionTracker, _clustering, this);
    }

    public SneakAttack.TerminalState CreateTerminalState() {
        return new SneakAttack.TerminalState(this);
    }
}
