namespace Bot.Managers.WarManagement.States.Finisher;

public class FinisherState : WarManagerState {
    private readonly IWarManagerBehaviourFactory _warManagerBehaviourFactory;

    private IWarManagerBehaviour _behaviour;

    public override IWarManagerBehaviour Behaviour => _behaviour;

    public FinisherState(IWarManagerBehaviourFactory warManagerBehaviourFactory) {
        _warManagerBehaviourFactory = warManagerBehaviourFactory;
    }

    protected override void OnContextSet() {
        _behaviour = _warManagerBehaviourFactory.CreateFinisherBehaviour(Context);
    }

    protected override void Execute() {
        // Nothing, this is a final state
    }

    protected override bool TryTransitioning() {
        // This is a final state
        return false;
    }
}
