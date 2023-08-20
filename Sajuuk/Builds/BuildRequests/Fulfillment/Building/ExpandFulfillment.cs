using Sajuuk.ExtensionMethods;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using SC2APIProtocol;

namespace Sajuuk.Builds.BuildRequests.Fulfillment.Building;

public sealed class ExpandFulfillment : BuildingFulfillment {
    private readonly IRegionsTracker _regionsTracker;

    public ExpandFulfillment(
        IUnitsTracker unitsTracker,
        IFrameClock frameClock,
        KnowledgeBase knowledgeBase,
        IRegionsTracker regionsTracker,
        Unit producer,
        UnitOrder producerOrder,
        uint buildingTypeToPlace
    ) : base(unitsTracker, frameClock, knowledgeBase, producer, producerOrder, buildingTypeToPlace) {
        _regionsTracker = regionsTracker;

        Status = BuildRequestFulfillmentStatus.Preparing;
    }

    public override bool CanSatisfy(IBuildRequest buildRequest) {
        if (buildRequest.BuildType is BuildType.Expand) {
            return false;
        }

        return buildRequest.UnitOrUpgradeType == BuildingTypeToPlace;
    }

    public override string ToString() {
        var expandPosition = ProducerOrder.TargetWorldSpacePos.ToVector2();
        var expandRegion = _regionsTracker.GetRegion(expandPosition);

        return $"Fulfillment {Producer} {BuildType.Expand.ToString()} in {expandRegion}";
    }
}
