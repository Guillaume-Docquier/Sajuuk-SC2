using System.Collections.Generic;
using Sajuuk.Builds;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.Managers.WarManagement.States;
using Sajuuk.StateManagement;

namespace Sajuuk.Managers.WarManagement;

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

public class WarManager : Manager {
    private readonly WarManagerDebugger _debugger;
    private readonly StateMachine<WarManager, WarManagerState> _stateMachine;

    protected override IAssigner Assigner => _stateMachine.State.Behaviour.Assigner;
    protected override IDispatcher Dispatcher => _stateMachine.State.Behaviour.Dispatcher;
    protected override IReleaser Releaser => _stateMachine.State.Behaviour.Releaser;

    public override IEnumerable<IFulfillableBuildRequest> BuildRequests => _stateMachine.State.Behaviour.BuildRequests;

    public WarManager(IWarManagerStateFactory warManagerStateFactory, IGraphicalDebugger graphicalDebugger) {
        _debugger = new WarManagerDebugger(graphicalDebugger);
        _stateMachine = new StateMachine<WarManager, WarManagerState>(
            this,
            warManagerStateFactory.CreateEarlyGameState()
        );
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
