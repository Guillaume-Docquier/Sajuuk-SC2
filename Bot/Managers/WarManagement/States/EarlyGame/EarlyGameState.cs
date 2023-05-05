﻿using Bot.Debugging;
using Bot.GameSense;
using Bot.GameSense.RegionsEvaluationsTracking;
using Bot.Managers.ScoutManagement;
using Bot.Tagging;
using Bot.Utils;

namespace Bot.Managers.WarManagement.States.EarlyGame;

public class EarlyGameState : WarManagerState {
    private const int EarlyGameEndInSeconds = (int)(5 * 60);

    private readonly ITaggingService _taggingService;
    private readonly IEnemyRaceTracker _enemyRaceTracker;
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IRegionsEvaluationsTracker _regionsEvaluationsTracker;
    private readonly IScoutSupervisorFactory _scoutSupervisorFactory;
    private readonly IWarSupervisorFactory _warSupervisorFactory;

    private TransitionState _transitionState = TransitionState.NotTransitioning;
    private IWarManagerBehaviour _behaviour;

    public EarlyGameState(
        ITaggingService taggingService,
        IEnemyRaceTracker enemyRaceTracker,
        IVisibilityTracker visibilityTracker,
        IDebuggingFlagsTracker debuggingFlagsTracker,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        IRegionsEvaluationsTracker regionsEvaluationsTracker,
        IScoutSupervisorFactory scoutSupervisorFactory,
        IWarSupervisorFactory warSupervisorFactory
    ) {
        _taggingService = taggingService;
        _enemyRaceTracker = enemyRaceTracker;
        _visibilityTracker = visibilityTracker;
        _debuggingFlagsTracker = debuggingFlagsTracker;
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _regionsTracker = regionsTracker;
        _regionsEvaluationsTracker = regionsEvaluationsTracker;
        _scoutSupervisorFactory = scoutSupervisorFactory;
        _warSupervisorFactory = warSupervisorFactory;
    }

    public override IWarManagerBehaviour Behaviour => _behaviour;

    protected override void OnContextSet() {
        _behaviour = new EarlyGameBehaviour(Context, _taggingService, _debuggingFlagsTracker, _unitsTracker, _regionsTracker, _regionsEvaluationsTracker, _warSupervisorFactory);
    }

    protected override void Execute() {
        if (_transitionState == TransitionState.NotTransitioning) {
            if (ShouldTransitionToMidGame()) {
                _transitionState = TransitionState.Transitioning;
            }
        }

        if (_transitionState == TransitionState.Transitioning) {
            TransitionToMidGame();
        }
    }

    protected override bool TryTransitioning() {
        if (_transitionState == TransitionState.TransitionComplete) {
            StateMachine.TransitionTo(new MidGame.MidGameState(
                _taggingService,
                _enemyRaceTracker,
                _visibilityTracker,
                _debuggingFlagsTracker,
                _unitsTracker,
                _terrainTracker,
                _regionsTracker,
                _regionsEvaluationsTracker,
                _scoutSupervisorFactory,
                _warSupervisorFactory
            ));

            return true;
        }

        return false;
    }

    private static bool ShouldTransitionToMidGame() {
        return Controller.Frame > TimeUtils.SecsToFrames(EarlyGameEndInSeconds);
    }

    private void TransitionToMidGame() {
        // TODO GD Wire the clean up sequence / mechanism? Figure that out anyways
        if (_behaviour.CleanUp()) {
            _transitionState = TransitionState.TransitionComplete;
        }
    }
}
