using System.Linq;
using Sajuuk.GameData;
using SC2APIProtocol;

namespace Sajuuk.Builds.BuildRequests.Fulfillment;

/// <summary>
/// TrainUnitFulfillment does not handle queued orders. Don't queue Train orders!
/// This is not be a huge problem for Zerg because only queens can be queued and since we're a bot we can avoid queuing.
/// i.e Hatchery with 5 queens queued, 1 of them gets canceled or finishes. How do you know which one was yours?
/// UnitOrders do not seem to have an id, so we can't easily reconcile which UnitOrder matches this fulfillment.
/// </summary>
public sealed class TrainUnitFulfillment : BuildRequestFulfillment {
    private readonly IFrameClock _frameClock;
    private readonly KnowledgeBase _knowledgeBase;

    private readonly Unit _producer;
    private readonly UnitOrder _producerOrder;
    private readonly uint _unitTypeToTrain;

    public TrainUnitFulfillment(
        IFrameClock frameClock,
        KnowledgeBase knowledgeBase,
        Unit producer,
        UnitOrder producerOrder,
        uint unitTypeToTrain
    ) {
        _frameClock = frameClock;
        _knowledgeBase = knowledgeBase;

        _producer = producer;
        _producerOrder = producerOrder;
        _unitTypeToTrain = unitTypeToTrain;

        // So far the BuildTime seems reliable and the unit completes after exactly that amount of frames.
        ExpectedCompletionFrame = frameClock.CurrentFrame + (ulong)knowledgeBase.GetUnitTypeData(unitTypeToTrain).BuildTime;

        Status = BuildRequestFulfillmentStatus.Executing;
    }

    public override ulong ExpectedCompletionFrame { get; }

    public override void UpdateStatus() {
        if (Status.HasFlag(BuildRequestFulfillmentStatus.Terminated)) {
            return;
        }

        if (_producer.IsDead(_frameClock.CurrentFrame)) {
            if (_frameClock.CurrentFrame < ExpectedCompletionFrame) {
                // The producer died before the expected completion frame, it was killed.
                Status = BuildRequestFulfillmentStatus.Prevented;
            }
            else {
                // The producer died at (or after) the expected completion frame
                // If the producer was a unit, it completed a morph!
                // If the producer was a building, I'm going to assume that it both died and produced the unit.
                Status = BuildRequestFulfillmentStatus.Completed;
            }
        }
        else if (Units.Buildings.Contains(_producer.UnitType) && _frameClock.CurrentFrame >= ExpectedCompletionFrame) {
            // The producer is a building so it didn't die (queens from hatcheries)
            Status = BuildRequestFulfillmentStatus.Completed;
        }
        else if (_producer.Orders.All(order => order.AbilityId != _producerOrder.AbilityId)) {
            Status = BuildRequestFulfillmentStatus.Aborted;
            Logger.Warning($"\"{this}\" because the producer has no matching order but was not canceled. This is not expected to happen.");
        }
    }

    public override bool CanSatisfy(IBuildRequest buildRequest) {
        if (buildRequest.BuildType != BuildType.Train) {
            return false;
        }

        return buildRequest.UnitOrUpgradeType == _unitTypeToTrain;
    }

    public override string ToString() {
        return $"Fulfillment {_producer} {BuildType.Train.ToString()} {_knowledgeBase.GetUnitTypeData(_unitTypeToTrain).Name} completing at {ExpectedCompletionFrame}";
    }
}
