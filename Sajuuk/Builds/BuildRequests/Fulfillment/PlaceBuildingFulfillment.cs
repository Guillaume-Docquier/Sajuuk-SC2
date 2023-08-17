using System.Linq;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using SC2APIProtocol;

namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public class PlaceBuildingFulfillment : BuildRequestFulfillment {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IFrameClock _frameClock;
    private readonly KnowledgeBase _knowledgeBase;

    private readonly Unit _producer;
    private readonly UnitOrder _producerOrder;
    private readonly uint _buildingTypeToPlace;
    private Unit _placedBuilding;

    public PlaceBuildingFulfillment(
        IUnitsTracker unitsTracker,
        IFrameClock frameClock,
        KnowledgeBase knowledgeBase,
        Unit producer,
        UnitOrder producerOrder,
        uint buildingTypeToPlace
    ) {
        _unitsTracker = unitsTracker;
        _frameClock = frameClock;
        _knowledgeBase = knowledgeBase;
        _producer = producer;
        _producerOrder = producerOrder;
        _buildingTypeToPlace = buildingTypeToPlace;

        Status = BuildRequestFulfillmentStatus.Preparing;
    }

    // TODO GD Implement this
    public override uint ExpectedCompletionFrame => 0;

    // TODO GD This could be a state machine
    // TODO GD We could also subscribe to unit deaths to make it simpler
    public override void UpdateStatus() {
        if (Status.HasFlag(BuildRequestFulfillmentStatus.Terminated)) {
            return;
        }

        if (Status == BuildRequestFulfillmentStatus.Executing) {
            if (_placedBuilding.IsDead(_frameClock.CurrentFrame)) {
                // The building was killed means it was prevented.
                Status = BuildRequestFulfillmentStatus.Prevented;
            }
            else if (_placedBuilding.IsOperational) {
                // The building is ready means we've completed.
                Status = BuildRequestFulfillmentStatus.Completed;
            }
        }
        else if (_producer.IsDead(_frameClock.CurrentFrame)) {
            var placedBuilding = _unitsTracker.NewOwnedUnits
                .Where(building => building.UnitType == _buildingTypeToPlace)
                .FirstOrDefault(building => {
                    // TODO GD Handle extractor (TargetUnitTag) vs building (TargetWorldSpacePos)
                    return building.Position.ToVector2() == _producerOrder.TargetWorldSpacePos.ToVector2();
                });

            if (placedBuilding != null) {
                // The producer ceased to exist and a building appeared at the ordered position means we built it.
                _placedBuilding = placedBuilding;
                Status = BuildRequestFulfillmentStatus.Executing;
            }
            else {
                // The producer was killed means it was prevented.
                // TODO GD Maybe we should check where the producer died?
                Status = BuildRequestFulfillmentStatus.Prevented;
            }
        }
        else if (!_producer.Orders.Any(OrderMatchesOurs)) {
            // The unit orders have changed means the producer received other orders.
            Status = BuildRequestFulfillmentStatus.Canceled;
        }

        // TODO GD Prevented if order fails (burrowed zergling, insufficient resources, invalid location)
    }

    public override bool CanSatisfy(IBuildRequest buildRequest) {
        if (buildRequest.BuildType != BuildType.Train) {
            return false;
        }

        return buildRequest.UnitOrUpgradeType == _buildingTypeToPlace;
    }

    /// <summary>
    /// Validates that the given unit order matches our expected unit order.
    /// TODO GD Can we just compare orders by value and not by reference? Will they even be the same?
    /// </summary>
    /// <param name="order">The unit order to validate.</param>
    /// <returns>True if the given order matches ours.</returns>
    private bool OrderMatchesOurs(UnitOrder order) {
        if (order.AbilityId != _producerOrder.AbilityId) {
            return false;
        }

        var isExtractor = Units.Extractors.Contains(_buildingTypeToPlace);
        if (isExtractor) {
            return order.TargetUnitTag == _producerOrder.TargetUnitTag;
        }

        return order.TargetWorldSpacePos.ToVector2() == _producerOrder.TargetWorldSpacePos.ToVector2();
    }

    public override string ToString() {
        return $"Fulfillment {_producer} {BuildType.Build.ToString()} {_knowledgeBase.GetUnitTypeData(_buildingTypeToPlace).Name}";
    }
}
