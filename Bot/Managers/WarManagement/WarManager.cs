using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.GameSense;
using Bot.Managers.WarManagement.States;
using Bot.Managers.WarManagement.States.EarlyGame;
using Bot.StateManagement;
using Bot.Tagging;

namespace Bot.Managers.WarManagement;

/*
 * REFACTOR NOTES
 *
 * Strategies themselves can use strategies or other states
 * - TerranFinisher
 * - CannonRush defense
 * - WorkerRush defense
 *
 * Be conservative about switching states try not to yo-yo
 */

public class WarManager: Manager {
    private readonly StateMachine<WarManager, WarManagerState> _stateMachine;
    private readonly WarManagerDebugger _debugger = new WarManagerDebugger();

    protected override IAssigner Assigner => _stateMachine.State.Behaviour.Assigner;
    protected override IDispatcher Dispatcher => _stateMachine.State.Behaviour.Dispatcher;
    protected override IReleaser Releaser => _stateMachine.State.Behaviour.Releaser;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _stateMachine.State.Behaviour.BuildRequests.Select(buildRequest => buildRequest.Fulfillment);

    public WarManager(ITaggingService taggingService, IEnemyRaceTracker enemyRaceTracker, IVisibilityTracker visibilityTracker) {
        _stateMachine = new StateMachine<WarManager, WarManagerState>(this, new EarlyGameState(taggingService, enemyRaceTracker, visibilityTracker));
    }

    public override string ToString() {
        return "WarManager";
    }

    protected override void StartOfFramePhase() {
        _stateMachine.OnFrame();
    }

    protected override void RecruitmentPhase() {
        _stateMachine.State.Behaviour.RecruitmentPhase();
    }

    protected override void DispatchPhase() {
        _stateMachine.State.Behaviour.DispatchPhase();
    }

    protected override void ManagementPhase() {
        _stateMachine.State.Behaviour.ManagementPhase();
        _debugger.Debug(ManagedUnits);
    }
}
