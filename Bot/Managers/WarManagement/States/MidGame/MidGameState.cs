using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameSense;

namespace Bot.Managers.WarManagement.States.MidGame;

public class MidGameState : WarManagerState {
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IWarManagerStateFactory _warManagerStateFactory;
    private readonly IWarManagerBehaviourFactory _warManagerBehaviourFactory;

    private TransitionState _transitionState = TransitionState.NotTransitioning;
    private IWarManagerBehaviour _behaviour;

    public MidGameState(
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IWarManagerStateFactory warManagerStateFactory,
        IWarManagerBehaviourFactory warManagerBehaviourFactory
    ) {
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _warManagerStateFactory = warManagerStateFactory;
        _warManagerBehaviourFactory = warManagerBehaviourFactory;
    }

    public override IWarManagerBehaviour Behaviour => _behaviour;

    protected override void OnContextSet() {
        _behaviour = _warManagerBehaviourFactory.CreateMidGameBehaviour(Context);
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
