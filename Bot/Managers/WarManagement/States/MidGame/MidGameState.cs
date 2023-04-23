using System.Linq;
using Bot.Debugging;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.Managers.WarManagement.States.Finisher;
using Bot.MapKnowledge;
using Bot.Tagging;

namespace Bot.Managers.WarManagement.States.MidGame;

public class MidGameState : WarManagerState {
    private readonly ITaggingService _taggingService;
    private readonly IEnemyRaceTracker _enemyRaceTracker;
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;

    private TransitionState _transitionState = TransitionState.NotTransitioning;
    private IWarManagerBehaviour _behaviour;

    public MidGameState(
        ITaggingService taggingService,
        IEnemyRaceTracker enemyRaceTracker,
        IVisibilityTracker visibilityTracker,
        IDebuggingFlagsTracker debuggingFlagsTracker
    ) {
        _taggingService = taggingService;
        _enemyRaceTracker = enemyRaceTracker;
        _visibilityTracker = visibilityTracker;
        _debuggingFlagsTracker = debuggingFlagsTracker;
    }

    public override IWarManagerBehaviour Behaviour => _behaviour;

    protected override void OnContextSet() {
        _behaviour = new MidGameBehaviour(Context, _visibilityTracker, _debuggingFlagsTracker);
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
            StateMachine.TransitionTo(new FinisherState(_taggingService, _enemyRaceTracker, _visibilityTracker, _debuggingFlagsTracker));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Evaluates if we are overwhelming the opponent.
    /// </summary>
    /// <returns>True if we can stop being fancy and just finish the opponent</returns>
    private bool ShouldTransitionToFinisher() {
        if (MapAnalyzer.Instance.VisibilityRatio < 0.85) {
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
    private static float GetEnemyForce() {
        return UnitsTracker.EnemyMemorizedUnits.Values.Concat(UnitsTracker.EnemyUnits).GetForce();
    }
}
