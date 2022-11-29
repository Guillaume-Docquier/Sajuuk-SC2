using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.Managers.WarManagement.States;
using Bot.Managers.WarManagement.States.EarlyGame;
using Bot.StateManagement;

namespace Bot.Managers.WarManagement;

/*
 * REFACTOR NOTES
 * Use a strategy to do each phase
 * - i.e EarlyGameRecruitmentPhaseStrategy
 *
 * Use a state machine to set the strategies, dispatchers, assigners and releasers
 *
 * Strategies themselves can use strategies or other states
 * - TerranFinisher
 * - CannonRush defense
 * - WorkerRush defense
 *
 * Be conservative about switching states try not to yo-yo
 */

public partial class WarManager: Manager {
    private readonly StateMachine<WarManager, WarManagerState> _stateMachine;

    protected override IAssigner Assigner => _stateMachine.State.Behaviour.Assigner;
    protected override IDispatcher Dispatcher => _stateMachine.State.Behaviour.Dispatcher;
    protected override IReleaser Releaser => _stateMachine.State.Behaviour.Releaser;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _stateMachine.State.Behaviour.BuildRequests.Select(buildRequest => buildRequest.Fulfillment);

    public WarManager() {
        _stateMachine = new StateMachine<WarManager, WarManagerState>(this, new EarlyGameState());
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
        // TODO GD Debug managed units
    }
}
