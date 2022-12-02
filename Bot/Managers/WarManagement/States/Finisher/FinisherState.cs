namespace Bot.Managers.WarManagement.States.Finisher;

public class FinisherState : WarManagerState {
    private IWarManagerBehaviour _behaviour;
    public override IWarManagerBehaviour Behaviour => _behaviour;

    protected override void OnContextSet() {
        _behaviour = new FinisherBehaviour(Context);
    }

    protected override void Execute() {
        // Nothing, this is a final state
    }

    protected override bool TryTransitioning() {
        // This is a final state
        return false;
    }
}
