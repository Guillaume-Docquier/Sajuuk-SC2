using System.Linq;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using SC2APIProtocol;

namespace Sajuuk.Builds.BuildRequests.Fulfillment.Building;

public sealed class PlaceExtractorFulfillment : PlaceBuildingFulfillment {
    private readonly Unit _gasGeyser;

    public PlaceExtractorFulfillment(
        IUnitsTracker unitsTracker,
        IFrameClock frameClock,
        KnowledgeBase knowledgeBase,
        ITimeToTravelCalculator timeToTravelCalculator,
        Unit producer,
        UnitOrder producerOrder,
        uint buildingTypeToPlace
    ) : base(unitsTracker, frameClock, knowledgeBase, timeToTravelCalculator, BuildType.Build, producer, producerOrder, buildingTypeToPlace) {
        _gasGeyser = unitsTracker.NeutralUnits.First(neutralUnit => neutralUnit.Tag == ProducerOrder.TargetUnitTag);
    }

    protected override bool OrderMatchesOurs(UnitOrder order) {
        if (order.AbilityId != ProducerOrder.AbilityId) {
            return false;
        }

        return order.TargetUnitTag == ProducerOrder.TargetUnitTag;
    }

    protected override bool BuildingMatchesOurs(Unit building) {
        if (building.UnitType != BuildingTypeToPlace) {
            return false;
        }

        var buildingPosition = building.Position.ToVector2();
        var expectedPosition = _gasGeyser.Position.ToVector2();

        return buildingPosition.DistanceTo(expectedPosition) < 0.15;
    }
}
