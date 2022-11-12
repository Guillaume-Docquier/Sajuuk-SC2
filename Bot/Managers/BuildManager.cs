using System.Collections.Generic;
using System.Linq;
using Bot.Builds;

namespace Bot.Managers;

public class BuildManager : UnitlessManager {
    private bool _buildOrderDoneAndTagged = false;
    private readonly IBuildOrder _buildOrder;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _buildOrder.BuildRequests.Select(buildRequest => buildRequest.Fulfillment);

    public BuildManager(IBuildOrder buildOrder) {
        _buildOrder = buildOrder;
    }

    protected override void ManagementPhase() {
        // TODO GD React to scouting
        _buildOrder.PruneRequests();

        // TODO GD Compare timing with implementation before the BuildManager
        if (!_buildOrderDoneAndTagged) {
            var buildOrderDone = _buildOrder.BuildRequests.All(request => request.Fulfillment.Remaining == 0);
            if (buildOrderDone) {
                TaggingService.TagGame(TaggingService.Tag.BuildDone);
                _buildOrderDoneAndTagged = true;
            }
        }
    }
}
