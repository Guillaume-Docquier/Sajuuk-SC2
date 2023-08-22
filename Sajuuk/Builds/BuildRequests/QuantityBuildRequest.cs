using System.Collections.Generic;
using System.Linq;
using Sajuuk.Builds.BuildRequests.Fulfillment;
using Sajuuk.GameData;

namespace Sajuuk.Builds.BuildRequests;

public class QuantityBuildRequest : BuildRequest {
    private readonly List<IBuildRequestFulfillment> _fulfillments = new List<IBuildRequestFulfillment>();

    // TODO GD This could cause a performance issues. If so, we should have fulfillments notify requests when their status changes
    public override int QuantityFulfilled => _fulfillments
        .Count(fulfillment => !fulfillment.Status.HasFlag(BuildRequestFulfillmentStatus.Failure));

    public QuantityBuildRequest(
        KnowledgeBase knowledgeBase,
        IController controller,
        IBuildRequestFulfillmentTracker buildRequestFulfillmentTracker,
        BuildType buildType,
        uint unitOrUpgradeType,
        int quantity,
        uint atSupply,
        bool queue,
        BuildBlockCondition blockCondition,
        BuildRequestPriority priority
    )
        : base(knowledgeBase, controller, buildRequestFulfillmentTracker, buildType, unitOrUpgradeType, quantity, atSupply, queue, blockCondition, priority) {}

    protected override void OnFulfillmentAdded(IBuildRequestFulfillment buildRequestFulfillment) {
        _fulfillments.Add(buildRequestFulfillment);
    }
}
