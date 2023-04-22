using Bot.GameSense;
using Bot.Tagging;

namespace Bot.Managers.WarManagement.States.Finisher;

public class FinisherState : WarManagerState {
    private readonly ITaggingService _taggingService;
    private readonly IEnemyRaceTracker _enemyRaceTracker;

    private IWarManagerBehaviour _behaviour;

    public override IWarManagerBehaviour Behaviour => _behaviour;

    public FinisherState(ITaggingService taggingService, IEnemyRaceTracker enemyRaceTracker) {
        _taggingService = taggingService;
        _enemyRaceTracker = enemyRaceTracker;
    }

    protected override void OnContextSet() {
        _behaviour = new FinisherBehaviour(Context, _taggingService, _enemyRaceTracker);
    }

    protected override void Execute() {
        // Nothing, this is a final state
    }

    protected override bool TryTransitioning() {
        // This is a final state
        return false;
    }
}
