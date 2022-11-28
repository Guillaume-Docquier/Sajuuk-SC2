using System.Collections.Generic;
using Bot.Builds;
using Bot.Managers.WarManagement.States;
using Bot.Managers.WarManagement.States.EarlyGame;
using Bot.StateManagement;

namespace Bot.Managers.WarManagement;

public class WarManagerBehaviour : IWarManagerBehaviour {
    private readonly StateMachine<WarManager, WarManagerState> _stateMachine;

    public IAssigner Assigner => _stateMachine.State.Behaviour.Assigner;
    public IDispatcher Dispatcher => _stateMachine.State.Behaviour.Dispatcher;
    public IReleaser Releaser => _stateMachine.State.Behaviour.Releaser;
    public List<BuildRequest> BuildRequests => _stateMachine.State.Behaviour.BuildRequests;

    public WarManagerBehaviour(WarManager warManager) {
        _stateMachine = new StateMachine<WarManager, WarManagerState>(warManager, new EarlyGameState());
    }

    public void Update() {
        _stateMachine.OnFrame();
    }

    public void RecruitmentPhase() {
        _stateMachine.State.Behaviour.RecruitmentPhase();
    }

    public void DispatchPhase() {
        _stateMachine.State.Behaviour.DispatchPhase();
    }

    public void ManagementPhase() {
        _stateMachine.State.Behaviour.ManagementPhase();
    }
}
