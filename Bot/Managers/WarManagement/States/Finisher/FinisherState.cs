using Bot.Tagging;

namespace Bot.Managers.WarManagement.States.Finisher;

public class FinisherState : WarManagerState {
    private IWarManagerBehaviour _behaviour;
    private readonly ITaggingService _taggingService;
    public override IWarManagerBehaviour Behaviour => _behaviour;

    public FinisherState(ITaggingService taggingService) {
        _taggingService = taggingService;
    }

    protected override void OnContextSet() {
        _behaviour = new FinisherBehaviour(Context, _taggingService);
    }

    protected override void Execute() {
        // Nothing, this is a final state
    }

    protected override bool TryTransitioning() {
        // This is a final state
        return false;
    }
}
