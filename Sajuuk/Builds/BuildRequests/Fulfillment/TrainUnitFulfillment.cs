using System.Linq;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using SC2APIProtocol;

namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public class TrainUnitFulfillment : BuildRequestFulfillment {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IFrameClock _frameClock;

    private readonly Unit _producer;
    private readonly UnitOrder _producerOrder;
    private readonly uint _unitTypeToTrain;

    public TrainUnitFulfillment(
        IUnitsTracker unitsTracker,
        IFrameClock frameClock,
        Unit producer,
        UnitOrder producerOrder,
        uint unitTypeToTrain
    ) {
        _unitsTracker = unitsTracker;
        _frameClock = frameClock;

        _producer = producer;
        _producerOrder = producerOrder;
        _unitTypeToTrain = unitTypeToTrain;
    }

    // TODO GD This might not be a huge problem for Zerg, but how do you deal with queued units?
    // TODO GD i.e Hatchery with 5 queens queued, 1 gets canceled or finishes. How do you know which one was yours?
    public override void UpdateStatus() {
        if (Status.HasFlag(BuildRequestFulfillmentStatus.Terminated)) {
            return;
        }

        // An order will be prevented by killing the producer
        // In the case of morphs, the producer will also die, but will spawn a new unit at its location
        if (_producer.IsDead(_frameClock.CurrentFrame)) {
            var trainedUnit = _unitsTracker.NewOwnedUnits
                .Where(unit => unit.UnitType == _unitTypeToTrain)
                // TODO GD Maybe the position is not going to be exactly the same
                .FirstOrDefault(unit => unit.Position.ToVector2() == _producer.Position.ToVector2());

            Status = trainedUnit == null
                ? BuildRequestFulfillmentStatus.Prevented
                : BuildRequestFulfillmentStatus.Completed;
        }
        else if (_producer.Orders.Any(order => order.AbilityId == _producerOrder.AbilityId)) {
            // TODO GD This is not exact when the unit is queued
            Status = BuildRequestFulfillmentStatus.Executing;
        }
        // Units spawning from buildings (queens from hatcheries)
        else if (Units.Buildings.Contains(_producer.UnitType)) {
            // We can do this because cancellation should go through the fulfillment.
            // If it wasn't cancelled, the producer is alive and there's no order, then it must be completed.
            // TODO GD What if the required tech building is destroyed while the unit is spawning?
            Status = BuildRequestFulfillmentStatus.Completed;
        }
        else {
            Status = BuildRequestFulfillmentStatus.Canceled;
        }
    }

    public override bool CanSatisfy(IBuildRequest buildRequest) {
        if (buildRequest.BuildType != BuildType.Train) {
            return false;
        }

        return buildRequest.UnitOrUpgradeType == _unitTypeToTrain;
    }
}
