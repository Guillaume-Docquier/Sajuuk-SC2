using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.GameSense.EnemyStrategyTracking;

namespace Bot.Managers;

public class BuildManager : UnitlessManager, ISubscriber<EnemyStrategyTransition> {
    private bool _buildOrderDoneAndTagged = false;
    private readonly IBuildOrder _buildOrder;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _buildOrder.BuildRequests.Select(buildRequest => buildRequest.Fulfillment);

    public BuildManager(IBuildOrder buildOrder) {
        _buildOrder = buildOrder;
        EnemyStrategyTracker.Instance.Register(this);
    }

    protected override void ManagementPhase() {
        _buildOrder.PruneRequests();

        if (!_buildOrderDoneAndTagged) {
            var buildOrderDone = _buildOrder.BuildRequests.All(request => request.Fulfillment.Remaining == 0);
            if (buildOrderDone) {
                var scoreDetails = Controller.Observation.Observation.Score.ScoreDetails;
                TaggingService.TagGame(TaggingService.Tag.BuildDone, Controller.CurrentSupply, scoreDetails.CollectedMinerals, scoreDetails.CollectedVespene);
                _buildOrderDoneAndTagged = true;
            }
        }
    }

    public void Notify(EnemyStrategyTransition data) {
        _buildOrder.ReactTo(data.CurrentStrategy);
    }
}
