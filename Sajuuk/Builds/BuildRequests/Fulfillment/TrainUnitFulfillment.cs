using System;
using System.Linq;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using SC2APIProtocol;

namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public class TrainUnitFulfillment : IBuildRequestFulfillment {
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

    private BuildRequestFulfillmentStatus _status = BuildRequestFulfillmentStatus.Preparing;
    public BuildRequestFulfillmentStatus Status {
        get => _status;
        private set {
            if (_status != value) {
                // TODO GD Include more info about the fulfillment
                Logger.Info($"Fulfillment {_status.ToString()}");
                _status = value;
            }
        }
    }

    public void UpdateStatus() {
        // TODO GD This might not be a problem for Zerg, but how do you deal with queued abilities?
        // TODO GD i.e Barracks with 5 marines queued, 1 gets canceled or finishes. How do you know which one was yours?
        // TODO GD Could be problem when training queens
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
            Status = BuildRequestFulfillmentStatus.Executing;
        }
        // Units spawning from buildings (queens from hatcheries)
        else if (Units.Buildings.Contains(_producer.UnitType)) {
            // We can do this because cancellation should go through the fulfillment.
            // If it wasn't cancelled, the producer is alive and there's no order, then it must be completed.
            // TODO GD What if the required tech building is destroyed while the unit is spawning?
            Status = BuildRequestFulfillmentStatus.Completed;
        }

        if (Status.HasFlag(BuildRequestFulfillmentStatus.Terminated)) {
            Logger.Info($"Fulfillment {Status.ToString()}");
        }
    }

    public void Abort() {
        Status = BuildRequestFulfillmentStatus.Aborted;
        Logger.Info($"Fulfillment {Status.ToString()}");
    }

    public void Cancel() {
        // TODO GD How to cancel?
        throw new NotImplementedException();

        Status = BuildRequestFulfillmentStatus.Canceled;
        Logger.Info($"Fulfillment {Status.ToString()}");
    }

    public bool CanSatisfy(IBuildRequest buildRequest) {
        if (buildRequest.BuildType != BuildType.Train) {
            return false;
        }

        return buildRequest.UnitOrUpgradeType == _unitTypeToTrain;
    }
}
