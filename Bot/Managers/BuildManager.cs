using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.GameSense.EnemyStrategyTracking;
using Bot.Tagging;

namespace Bot.Managers;

public class BuildManager : UnitlessManager, ISubscriber<EnemyStrategyTransition> {
    private readonly IBuildOrder _buildOrder;
    private readonly ITaggingService _taggingService;

    public bool IsBuildOrderDone { get; private set; } = false;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _buildOrder.BuildRequests.Select(buildRequest => buildRequest.Fulfillment);

    public BuildManager(IBuildOrder buildOrder, ITaggingService taggingService, IEnemyStrategyTracker enemyStrategyTracker) {
        _buildOrder = buildOrder;
        _taggingService = taggingService;
        enemyStrategyTracker.Register(this);
    }

    protected override void ManagementPhase() {
        _buildOrder.PruneRequests();

        if (!IsBuildOrderDone && _buildOrder.BuildRequests.All(request => request.Fulfillment.Remaining <= 0)) {
            var scoreDetails = Controller.Observation.Observation.Score.ScoreDetails;
            _taggingService.TagBuildDone(Controller.CurrentSupply, scoreDetails.CollectedMinerals, scoreDetails.CollectedVespene);
            IsBuildOrderDone = true;
        }
    }

    public void Notify(EnemyStrategyTransition data) {
        _buildOrder.ReactTo(data.CurrentStrategy);
    }
}
