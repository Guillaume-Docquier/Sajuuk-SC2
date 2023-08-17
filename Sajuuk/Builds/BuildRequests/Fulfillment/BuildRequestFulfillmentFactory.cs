using Sajuuk.GameSense;
using SC2APIProtocol;

namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public class BuildRequestFulfillmentFactory : IBuildRequestFulfillmentFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IFrameClock _frameClock;

    public BuildRequestFulfillmentFactory(
        IUnitsTracker unitsTracker,
        IFrameClock frameClock
    ) {
        _unitsTracker = unitsTracker;
        _frameClock = frameClock;
    }

    public IBuildRequestFulfillment CreateTrainUnitFulfillment(Unit producer, UnitOrder producerOrder, uint unitTypeToTrain) {
        return new TrainUnitFulfillment(
            _unitsTracker, _frameClock,
            producer, producerOrder, unitTypeToTrain
        );
    }

    public IBuildRequestFulfillment CreatePlaceBuildingFulfillment(Unit producer, UnitOrder producerOrder, uint buildingTypeToPlace) {
        return new PlaceBuildingFulfillment(
            _unitsTracker, _frameClock,
            producer, producerOrder, buildingTypeToPlace
        );
    }
}
