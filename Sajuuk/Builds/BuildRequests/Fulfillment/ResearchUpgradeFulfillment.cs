using System.Linq;
using Sajuuk.GameData;
using SC2APIProtocol;

namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public class ResearchUpgradeFulfillment : BuildRequestFulfillment {
    private readonly IFrameClock _frameClock;
    private readonly KnowledgeBase _knowledgeBase;
    private readonly IController _controller;

    private readonly Unit _producer;
    private readonly UnitOrder _producerOrder;
    private readonly uint _upgradeTypeToResearch;

    private readonly ulong _researchTime;

    public ResearchUpgradeFulfillment(
        IFrameClock frameClock,
        KnowledgeBase knowledgeBase,
        IController controller,
        Unit producer,
        UnitOrder producerOrder,
        uint upgradeTypeToResearch
    ) {
        _frameClock = frameClock;
        _knowledgeBase = knowledgeBase;
        _controller = controller;

        _producer = producer;
        _producerOrder = producerOrder;
        _upgradeTypeToResearch = upgradeTypeToResearch;

        _researchTime = (ulong)_knowledgeBase.GetUpgradeData(_upgradeTypeToResearch).ResearchTime;

        SetProgressStatus();
    }

    private ulong _expectedCompletionFrame;
    public override ulong ExpectedCompletionFrame => _expectedCompletionFrame;

    /// <summary>
    /// Updates the status and the expected completion frame.
    /// </summary>
    public override void UpdateStatus() {
        if (Status.HasFlag(BuildRequestFulfillmentStatus.Terminated)) {
            return;
        }

        if (_controller.ResearchedUpgrades.Contains(_upgradeTypeToResearch)) {
            // You can't research the same thing twice, so if the research is done it has to be because of this fulfillment.
            Status = BuildRequestFulfillmentStatus.Completed;
        }
        else if (_producer.IsDead(_frameClock.CurrentFrame)) {
            Status = BuildRequestFulfillmentStatus.Prevented;
        }
        else if (!_producer.Orders.Any(OrderMatchesOurs)) {
            // Cancel should go through us, so we'll assume not finding the order means it was aborted by the game.
            Status = BuildRequestFulfillmentStatus.Aborted;
        }
        else if (Status == BuildRequestFulfillmentStatus.Preparing) {
            SetProgressStatus();
        }
    }

    /// <summary>
    /// A build request can be satisfied by this fulfillment if it is a research of the same upgrade type.
    /// </summary>
    /// <param name="buildRequest">The build request to validate against.</param>
    /// <returns>True if this fulfillment could satisfy the given build request.</returns>
    public override bool CanSatisfy(IBuildRequest buildRequest) {
        if (buildRequest.BuildType != BuildType.Research) {
            return false;
        }

        return buildRequest.UnitOrUpgradeType == _upgradeTypeToResearch;
    }

    public override string ToString() {
        return $"Fulfillment {_producer} {BuildType.Research.ToString()} {_knowledgeBase.GetUpgradeData(_upgradeTypeToResearch).Name} completing at {ExpectedCompletionFrame}";
    }

    /// <summary>
    /// Sets the status to Preparing or Executing depending on if the research is queued or not and updates the expected completion frame accordingly.
    /// </summary>
    private void SetProgressStatus() {
        if (OrderMatchesOurs(_producer.Orders.First())) {
            _expectedCompletionFrame = _frameClock.CurrentFrame + _researchTime;
            Status = BuildRequestFulfillmentStatus.Executing;
        }
        else {
            // We are queued, let's update the completion frame
            _expectedCompletionFrame = EstimateCompletionFrame();
            Status = BuildRequestFulfillmentStatus.Preparing;
        }
    }

    /// <summary>
    /// Validates that the given unit order matches our expected unit order.
    /// </summary>
    /// <param name="order">The unit order to validate.</param>
    /// <returns>True if the given order matches ours.</returns>
    private bool OrderMatchesOurs(UnitOrder order) {
        return order.AbilityId == _producerOrder.AbilityId;
    }

    /// <summary>
    /// Estimates the completion frame based on the producer orders.
    /// It will take into account queued orders before our own.
    /// </summary>
    /// <returns>The frame at which our research will have been researched.</returns>
    private ulong EstimateCompletionFrame() {
        var ordersToComplete = _producer.Orders.TakeWhile(order => !OrderMatchesOurs(order));
        var timeToComplete = (ulong)(ordersToComplete.Sum(ComputeRemainingResearchTime) + _researchTime);

        return _frameClock.CurrentFrame + timeToComplete;
    }

    /// <summary>
    /// Computes the remaining research time of the given research order based on its current progress.
    /// </summary>
    /// <param name="researchOrder">The research order to compute the remaining time of.</param>
    /// <returns>The remaining research time of the given research order.</returns>
    private double ComputeRemainingResearchTime(UnitOrder researchOrder) {
        var researchTime = _knowledgeBase.GetUpgradeDataFromAbilityId(researchOrder.AbilityId).ResearchTime;
        var remainingProgress = 1 - researchOrder.Progress;

        return researchTime * remainingProgress;
    }
}
