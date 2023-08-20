using Sajuuk.GameData;
using Sajuuk.GameSense;
using SC2APIProtocol;

namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public sealed class PlaceBuildingFulfillment : BuildingFulfillment {
    public PlaceBuildingFulfillment(
        IUnitsTracker unitsTracker,
        IFrameClock frameClock,
        KnowledgeBase knowledgeBase,
        Unit producer,
        UnitOrder producerOrder,
        uint buildingTypeToPlace
    ) : base(unitsTracker, frameClock, knowledgeBase, producer, producerOrder, buildingTypeToPlace) {
        Status = BuildRequestFulfillmentStatus.Preparing;
    }
}
