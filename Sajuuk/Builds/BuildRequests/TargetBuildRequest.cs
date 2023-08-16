using System.Linq;
using Sajuuk.Builds.BuildRequests.Fulfillment;
using Sajuuk.GameData;
using Sajuuk.GameSense;

namespace Sajuuk.Builds.BuildRequests;

public class TargetBuildRequest : BuildRequest {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IBuildRequestFulfillmentTracker _buildRequestFulfillmentTracker;
    private readonly IController _controller;

    public override int QuantityFulfilled => ComputeQuantityFulfilled();

    public TargetBuildRequest(
        KnowledgeBase knowledgeBase,
        IController controller,
        IUnitsTracker unitsTracker,
        IBuildRequestFulfillmentTracker buildRequestFulfillmentTracker,
        BuildType buildType,
        uint unitOrUpgradeType,
        int targetQuantity,
        uint atSupply,
        bool queue,
        BuildBlockCondition blockCondition,
        BuildRequestPriority priority
    )
        : base(knowledgeBase, controller, buildType, unitOrUpgradeType, targetQuantity, atSupply, queue, blockCondition, priority) {
        _controller = controller;
        _unitsTracker = unitsTracker;
        _buildRequestFulfillmentTracker = buildRequestFulfillmentTracker;
    }

    public override void AddFulfillment(IBuildRequestFulfillment buildRequestFulfillment) {
        // Target build requests must consider the global scope, so their own buildRequestFulfillments don't matter.
    }

    private int ComputeQuantityFulfilled() {
        var nbFulfillmentsInProgress = _buildRequestFulfillmentTracker
            .FulfillmentsInProgress
            .Count(fulfillment => fulfillment.CanSatisfy(this));

        if (BuildType == BuildType.Research) {
            return _controller.ResearchedUpgrades.Contains(UnitOrUpgradeType) || nbFulfillmentsInProgress > 0 ? 1 : 0;
        }

        var existingUnitsOrBuildings = _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, UnitOrUpgradeType);
        if (Units.Extractors.Contains(UnitOrUpgradeType)) {
            // TODO GD When losing the natural, this causes the build to be stuck because it will try to replace gasses that are already taken
            // Ignore extractors that are not assigned to a townhall. This way we can target X working extractors
            existingUnitsOrBuildings = existingUnitsOrBuildings.Where(extractor => extractor.Supervisor != null);
        }

        return existingUnitsOrBuildings.Count() + nbFulfillmentsInProgress;
    }
}
