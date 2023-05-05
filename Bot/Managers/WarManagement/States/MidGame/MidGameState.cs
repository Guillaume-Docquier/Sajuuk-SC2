using System.Linq;
using Bot.Debugging;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.GameSense.RegionsEvaluationsTracking;
using Bot.Managers.ScoutManagement;

namespace Bot.Managers.WarManagement.States.MidGame;

public class MidGameState : WarManagerState {
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IRegionsEvaluationsTracker _regionsEvaluationsTracker;
    private readonly IScoutSupervisorFactory _scoutSupervisorFactory;
    private readonly IWarSupervisorFactory _warSupervisorFactory;
    private readonly IWarManagerStateFactory _warManagerStateFactory;

    private TransitionState _transitionState = TransitionState.NotTransitioning;
    private IWarManagerBehaviour _behaviour;

    public MidGameState(
        IVisibilityTracker visibilityTracker,
        IDebuggingFlagsTracker debuggingFlagsTracker,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        IRegionsEvaluationsTracker regionsEvaluationsTracker,
        IScoutSupervisorFactory scoutSupervisorFactory,
        IWarSupervisorFactory warSupervisorFactory,
        IWarManagerStateFactory warManagerStateFactory
    ) {
        _visibilityTracker = visibilityTracker;
        _debuggingFlagsTracker = debuggingFlagsTracker;
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _regionsTracker = regionsTracker;
        _regionsEvaluationsTracker = regionsEvaluationsTracker;
        _scoutSupervisorFactory = scoutSupervisorFactory;
        _warSupervisorFactory = warSupervisorFactory;
        _warManagerStateFactory = warManagerStateFactory;
    }

    public override IWarManagerBehaviour Behaviour => _behaviour;

    protected override void OnContextSet() {
        _behaviour = new MidGameBehaviour(Context, _visibilityTracker, _debuggingFlagsTracker, _unitsTracker, _terrainTracker, _regionsTracker, _regionsEvaluationsTracker, _scoutSupervisorFactory, _warSupervisorFactory);
    }

    protected override void Execute() {
        if (_transitionState == TransitionState.NotTransitioning) {
            if (ShouldTransitionToFinisher()) {
                _transitionState = TransitionState.Transitioning;
            }
        }

        if (_transitionState == TransitionState.Transitioning) {
            TransitionToFinisher();
        }
    }

    protected override bool TryTransitioning() {
        if (_transitionState == TransitionState.TransitionComplete) {
            StateMachine.TransitionTo(_warManagerStateFactory.CreateFinisherState());

            return true;
        }

        return false;
    }

    /// <summary>
    /// Evaluates if we are overwhelming the opponent.
    /// </summary>
    /// <returns>True if we can stop being fancy and just finish the opponent</returns>
    private bool ShouldTransitionToFinisher() {
        if (_terrainTracker.VisibilityRatio < 0.85) {
            return false;
        }

        var ourForce = Context.ManagedUnits.GetForce();
        var enemyForce = GetEnemyForce();
        if (ourForce < enemyForce * 3) {
            return false;
        }

        return true;
    }

    private void TransitionToFinisher() {
        if (_behaviour.CleanUp()) {
            _transitionState = TransitionState.TransitionComplete;
        }
    }

    /// <summary>
    /// Returns the enemy force
    /// </summary>
    /// <returns></returns>
    private float GetEnemyForce() {
        return _unitsTracker.EnemyMemorizedUnits.Values.Concat(_unitsTracker.EnemyUnits).GetForce();
    }
}
