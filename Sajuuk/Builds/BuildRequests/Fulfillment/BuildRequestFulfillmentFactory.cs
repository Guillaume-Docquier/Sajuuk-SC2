using Sajuuk.Builds.BuildRequests.Fulfillment.Building;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using SC2APIProtocol;

namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public class BuildRequestFulfillmentFactory : IBuildRequestFulfillmentFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IFrameClock _frameClock;
    private readonly KnowledgeBase _knowledgeBase;
    private readonly IRegionsTracker _regionsTracker;

    public BuildRequestFulfillmentFactory(
        IUnitsTracker unitsTracker,
        IFrameClock frameClock,
        KnowledgeBase knowledgeBase,
        IRegionsTracker regionsTracker
    ) {
        _unitsTracker = unitsTracker;
        _frameClock = frameClock;
        _knowledgeBase = knowledgeBase;
        _regionsTracker = regionsTracker;
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

    public IBuildRequestFulfillment CreatePlaceExtractorFulfillment(Unit producer, UnitOrder producerOrder, uint extractorTypeToPlace) {
        return new PlaceExtractorFulfillment(
            _unitsTracker, _frameClock, _knowledgeBase,
            producer, producerOrder, extractorTypeToPlace
        );
    }

    public IBuildRequestFulfillment CreateExpandFulfillment(Unit producer, UnitOrder producerOrder, uint expandTypeToPlace) {
        return new ExpandFulfillment(
            _unitsTracker, _frameClock, _knowledgeBase, _regionsTracker,
            producer, producerOrder, expandTypeToPlace
        );
    }
}
