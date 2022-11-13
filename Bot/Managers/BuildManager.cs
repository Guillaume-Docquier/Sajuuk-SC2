using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.GameSense.EnemyStrategyTracking;

namespace Bot.Managers;

public class BuildManager : UnitlessManager {
    private bool _buildOrderDoneAndTagged = false;
    private readonly IBuildOrder _buildOrder;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _buildOrder.BuildRequests.Select(buildRequest => buildRequest.Fulfillment);

    public BuildManager(IBuildOrder buildOrder) {
        _buildOrder = buildOrder;
    }

    protected override void ManagementPhase() {
        _buildOrder.ReactTo(EnemyStrategyTracker.EnemyStrategy);
        _buildOrder.PruneRequests();

        if (!_buildOrderDoneAndTagged) {
            var buildOrderDone = _buildOrder.BuildRequests.All(request => request.Fulfillment.Remaining == 0);
            if (buildOrderDone) {
                TaggingService.TagGame(TaggingService.Tag.BuildDone);
                _buildOrderDoneAndTagged = true;
            }
        }
    }
}
