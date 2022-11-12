using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.Utils;

namespace Bot.Managers.BuildManagement;

public partial class BuildManager : Manager {
    private bool _buildOrderDoneAndTagged = false;
    private readonly IBuildOrder _buildOrder;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _buildOrder.BuildRequests.Select(buildRequest => buildRequest.Fulfillment);

    protected override IAssigner Assigner { get; }
    protected override IDispatcher Dispatcher { get; }
    protected override IReleaser Releaser { get; }

    public BuildManager(IBuildOrder buildOrder) {
        _buildOrder = buildOrder;

        Assigner = new BuildManagerAssigner(this);
        Dispatcher = new BuildManagerDispatcher(this);
        Releaser = new BuildManagerReleaser(this);
    }

    protected override void AssignmentPhase() {}

    protected override void DispatchPhase() {}

    protected override void ManagementPhase() {
        // TODO GD React to scouting
        _buildOrder.PruneRequests();

        // TODO GD Compare timing with implementation before the BuildManager
        if (!_buildOrderDoneAndTagged) {
            var buildOrderDone = _buildOrder.BuildRequests.All(request => request.Fulfillment.Remaining == 0);
            if (buildOrderDone) {
                Controller.TagGame($"BuildDone_{TimeUtils.GetGameTimeString()}");
                _buildOrderDoneAndTagged = true;
            }
        }
    }
}
