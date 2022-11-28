using Bot.Managers.WarManagement.States;
using Bot.Managers.WarManagement.States.EarlyGame;
using Bot.StateManagement;

namespace Bot.Managers.WarManagement;

public class WarManagerBehaviour {
    private readonly StateMachine<WarManagerBehaviour> _stateMachine;

    public readonly WarManager WarManager;

    public WarManagerStrategy RecruitmentPhaseStrategy;
    public WarManagerStrategy DispatchPhaseStrategy;
    public WarManagerStrategy ManagementPhaseStrategy;

    public IAssigner Assigner;
    public IDispatcher Dispatcher;
    public IReleaser Releaser;

    public WarManagerBehaviour(WarManager warManager) {
        _stateMachine = new StateMachine<WarManagerBehaviour>(this, new EarlyGameState());

        WarManager = warManager;
    }

    public void Update() {
        _stateMachine.OnFrame();
    }
}
