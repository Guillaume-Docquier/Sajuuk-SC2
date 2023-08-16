using Sajuuk.Builds.BuildRequests.Fulfillment;
using Sajuuk.GameData;
using Sajuuk.GameSense;

namespace Sajuuk.Builds.BuildRequests;

public class BuildRequestFactory : IBuildRequestFactory {
    private readonly KnowledgeBase _knowledgeBase;
    private readonly IController _controller;
    private readonly IUnitsTracker _unitsTracker;
    private readonly IBuildRequestFulfillmentTracker _buildRequestFulfillmentTracker;

    public BuildRequestFactory(
        KnowledgeBase knowledgeBase,
        IController controller,
        IUnitsTracker unitsTracker,
        IBuildRequestFulfillmentTracker buildRequestFulfillmentTracker
    ) {
        _knowledgeBase = knowledgeBase;
        _unitsTracker = unitsTracker;
        _buildRequestFulfillmentTracker = buildRequestFulfillmentTracker;
        _controller = controller;
    }

    public TargetBuildRequest CreateTargetBuildRequest(
        BuildType buildType,
        uint unitOrUpgradeType,
        int targetQuantity,
        uint atSupply = 0,
        bool queue = false,
        BuildBlockCondition blockCondition = BuildBlockCondition.None,
        BuildRequestPriority priority = BuildRequestPriority.Normal
    ) {
        return new TargetBuildRequest(
            _knowledgeBase, _controller, _unitsTracker, _buildRequestFulfillmentTracker,
            buildType, unitOrUpgradeType, targetQuantity, atSupply, queue, blockCondition, priority
        );
    }

    public QuantityBuildRequest CreateQuantityBuildRequest(
        BuildType buildType,
        uint unitOrUpgradeType,
        int quantity = 1,
        uint atSupply = 0,
        bool queue = false,
        BuildBlockCondition blockCondition = BuildBlockCondition.None,
        BuildRequestPriority priority = BuildRequestPriority.Normal
    ) {
        return new QuantityBuildRequest(_knowledgeBase, _controller, buildType, unitOrUpgradeType, quantity, atSupply, queue, blockCondition, priority);
    }
}
