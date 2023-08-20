using System;
using System.Linq;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using SC2APIProtocol;

namespace Sajuuk.Builds.BuildRequests.Fulfillment.Building;

public abstract class BuildingFulfillment : BuildRequestFulfillment {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IFrameClock _frameClock;
    private readonly KnowledgeBase _knowledgeBase;
    private readonly ITimeToTravelCalculator _timeToTravelCalculator;

    protected readonly Unit Producer;
    protected readonly UnitOrder ProducerOrder;
    protected readonly uint BuildingTypeToPlace;
    private Unit _placedBuilding;

    private readonly uint _creationFrame;
    private uint _timeToTravel;
    private readonly uint _buildTime;

    protected BuildingFulfillment(
        IUnitsTracker unitsTracker,
        IFrameClock frameClock,
        KnowledgeBase knowledgeBase,
        ITimeToTravelCalculator timeToTravelCalculator,
        Unit producer,
        UnitOrder producerOrder,
        uint buildingTypeToPlace
    ) {
        _unitsTracker = unitsTracker;
        _frameClock = frameClock;
        _knowledgeBase = knowledgeBase;
        _timeToTravelCalculator = timeToTravelCalculator;

        Producer = producer;
        ProducerOrder = producerOrder;
        BuildingTypeToPlace = buildingTypeToPlace;

        _creationFrame = frameClock.CurrentFrame;
        _timeToTravel = _timeToTravelCalculator.CalculateTimeToTravel();
        // So far the BuildTime seems reliable and the building completes after exactly that amount of frames.
        _buildTime = (uint)knowledgeBase.GetUnitTypeData(buildingTypeToPlace).BuildTime;
    }

    public override uint ExpectedCompletionFrame => _creationFrame + _timeToTravel + _buildTime;

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
        else if (Producer.IsDead(_frameClock.CurrentFrame)) {
            var placedBuilding = _unitsTracker.NewOwnedUnits.FirstOrDefault(BuildingMatchesOurs);
            if (placedBuilding == null) {
                // The producer was killed without placing a building means it was prevented.
                // TODO GD Maybe we should check where the producer died?
                Status = BuildRequestFulfillmentStatus.Prevented;
            }
            else {
                // The producer ceased to exist and a building appeared at the ordered position means we built it.
                _placedBuilding = placedBuilding;
                Status = BuildRequestFulfillmentStatus.Executing;
            }
        }
        else if (!Producer.Orders.Any(OrderMatchesOurs)) {
            // Maybe the unit received other orders
            Status = BuildRequestFulfillmentStatus.Canceled;
        }

        _timeToTravel = _timeToTravelCalculator.CalculateTimeToTravel();

        // TODO GD Prevented if order fails (burrowed enemy unit, insufficient resources, invalid location)
    }

    public override bool CanSatisfy(IBuildRequest buildRequest) {
        if (buildRequest.BuildType is BuildType.Build) {
            return false;
        }

        return buildRequest.UnitOrUpgradeType == BuildingTypeToPlace;
    }

    /// <summary>
    /// Validates that the given unit order matches our expected unit order.
    /// </summary>
    /// <param name="order">The unit order to validate.</param>
    /// <returns>True if the given order matches ours.</returns>
    protected virtual bool OrderMatchesOurs(UnitOrder order) {
        if (order.AbilityId != ProducerOrder.AbilityId) {
            return false;
        }

        return order.TargetWorldSpacePos.ToVector2() == ProducerOrder.TargetWorldSpacePos.ToVector2();
    }

    /// <summary>
    /// Validates that the given building matches the building that we're expecting to place.
    /// </summary>
    /// <param name="building">The new building to validate.</param>
    /// <returns>True if the given building might have been placed for this fulfillment.</returns>
    protected virtual bool BuildingMatchesOurs(Unit building) {
        if (building.UnitType != BuildingTypeToPlace) {
            return false;
        }

        var buildingPosition = building.Position.ToVector2();
        var expectedPosition = ProducerOrder.TargetWorldSpacePos.ToVector2();

        // TODO GD spines and spores (or any building that doesn't have an odd x odd footprint) will probably have a large distance difference
        // We could filter more strictly for buildings that are not spines or spores
        return buildingPosition.DistanceTo(expectedPosition) < 0.6;
    }

    public override string ToString() {
        return $"Fulfillment {Producer} {BuildType.Build.ToString()} {_knowledgeBase.GetUnitTypeData(BuildingTypeToPlace).Name} completing at {ExpectedCompletionFrame}";
    }
}
