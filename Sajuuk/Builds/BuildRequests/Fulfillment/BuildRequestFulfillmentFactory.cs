using System.Linq;
using Sajuuk.Builds.BuildRequests.Fulfillment.Building;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis;
using SC2APIProtocol;

namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public class BuildRequestFulfillmentFactory : IBuildRequestFulfillmentFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IFrameClock _frameClock;
    private readonly KnowledgeBase _knowledgeBase;
    private readonly IPathfinder _pathfinder;
    private readonly FootprintCalculator _footprintCalculator;
    private readonly ITerrainTracker _terrainTracker;

    public BuildRequestFulfillmentFactory(
        IUnitsTracker unitsTracker,
        IFrameClock frameClock,
        KnowledgeBase knowledgeBase,
        IPathfinder pathfinder,
        FootprintCalculator footprintCalculator,
        ITerrainTracker terrainTracker
    ) {
        _unitsTracker = unitsTracker;
        _frameClock = frameClock;
        _knowledgeBase = knowledgeBase;
        _pathfinder = pathfinder;
        _footprintCalculator = footprintCalculator;
        _terrainTracker = terrainTracker;
    }

    public IBuildRequestFulfillment CreateTrainUnitFulfillment(Unit producer, UnitOrder producerOrder, uint unitTypeToTrain) {
        return new TrainUnitFulfillment(
            _frameClock, _knowledgeBase,
            producer, producerOrder, unitTypeToTrain
        );
    }

    public IBuildRequestFulfillment CreatePlaceBuildingFulfillment(Unit producer, UnitOrder producerOrder, uint buildingTypeToPlace) {
        var timeToTravelCalculator = new PlaceBuildingTimeToTravelCalculator(_pathfinder, producer, producerOrder.TargetWorldSpacePos.ToVector2());

        return new PlaceBuildingFulfillment(
            _unitsTracker, _frameClock, _knowledgeBase, timeToTravelCalculator, BuildType.Build,
            producer, producerOrder, buildingTypeToPlace
        );
    }

    public IBuildRequestFulfillment CreatePlaceExtractorFulfillment(Unit producer, UnitOrder producerOrder, uint extractorTypeToPlace) {
        var gasGeyser = _unitsTracker.NeutralUnits.First(neutralUnit => neutralUnit.Tag == producerOrder.TargetUnitTag);
        var timeToTravelCalculator = new PlaceExtractorTimeToTravelCalculator(_pathfinder, _footprintCalculator, _terrainTracker, producer, gasGeyser);

        return new PlaceExtractorFulfillment(
            _unitsTracker, _frameClock, _knowledgeBase, timeToTravelCalculator,
            producer, producerOrder, extractorTypeToPlace
        );
    }

    public IBuildRequestFulfillment CreateExpandFulfillment(Unit producer, UnitOrder producerOrder, uint expandTypeToPlace) {
        var timeToTravelCalculator = new PlaceBuildingTimeToTravelCalculator(_pathfinder, producer, producerOrder.TargetWorldSpacePos.ToVector2());

        return new PlaceBuildingFulfillment(
            _unitsTracker, _frameClock, _knowledgeBase, timeToTravelCalculator, BuildType.Expand,
            producer, producerOrder, expandTypeToPlace
        );
    }
}
