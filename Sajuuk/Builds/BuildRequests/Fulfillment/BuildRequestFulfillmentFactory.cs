using Sajuuk.GameData;
using Sajuuk.GameSense;
using SC2APIProtocol;

namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public class BuildRequestFulfillmentFactory : IBuildRequestFulfillmentFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IFrameClock _frameClock;
    private readonly KnowledgeBase _knowledgeBase;

    public BuildRequestFulfillmentFactory(
        IUnitsTracker unitsTracker,
        IFrameClock frameClock,
        KnowledgeBase knowledgeBase
    ) {
        _unitsTracker = unitsTracker;
        _frameClock = frameClock;
        _knowledgeBase = knowledgeBase;
    }

    public IBuildRequestFulfillment CreateTrainUnitFulfillment(Unit producer, UnitOrder producerOrder, uint unitTypeToTrain) {
        return new TrainUnitFulfillment(
            _frameClock, _knowledgeBase,
            producer, producerOrder, unitTypeToTrain
        );
    }

    public IBuildRequestFulfillment CreatePlaceBuildingFulfillment(Unit producer, UnitOrder producerOrder, uint buildingTypeToPlace) {
        return new PlaceBuildingFulfillment(
            _unitsTracker, _frameClock, _knowledgeBase,
            producer, producerOrder, buildingTypeToPlace
        );
    }
}
