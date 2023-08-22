using System;
using Sajuuk.Builds.BuildRequests.Fulfillment;
using Sajuuk.GameData;

namespace Sajuuk.Builds.BuildRequests;

public abstract class BuildRequest : IBuildRequest {
    private readonly KnowledgeBase _knowledgeBase;
    private readonly IController _controller;
    private readonly IBuildRequestFulfillmentTracker _buildRequestFulfillmentTracker;

    public BuildType BuildType { get; }
    public uint UnitOrUpgradeType { get; }
    public int QuantityRequested { get; set; }
    public uint AtSupply { get; set; }
    public bool AllowQueueing { get; }
    public BuildBlockCondition BlockCondition { get; set; }
    public BuildRequestPriority Priority { get; set; }

    public int QuantityRemaining => Math.Max(0, QuantityRequested - QuantityFulfilled);

    protected BuildRequest(
        KnowledgeBase knowledgeBase,
        IController controller,
        IBuildRequestFulfillmentTracker buildRequestFulfillmentTracker,
        BuildType buildType,
        uint unitOrUpgradeType,
        int quantity,
        uint atSupply,
        bool allowQueueing,
        BuildBlockCondition blockCondition,
        BuildRequestPriority priority
    ) {
        _knowledgeBase = knowledgeBase;
        _controller = controller;
        _buildRequestFulfillmentTracker = buildRequestFulfillmentTracker;

        BuildType = buildType;
        UnitOrUpgradeType = unitOrUpgradeType;
        AtSupply = atSupply;
        QuantityRequested = quantity;
        AllowQueueing = allowQueueing;
        BlockCondition = blockCondition;
        Priority = priority;
    }

    public abstract int QuantityFulfilled { get; }

    public void AddFulfillment(IBuildRequestFulfillment buildRequestFulfillment) {
        _buildRequestFulfillmentTracker.TrackFulfillment(buildRequestFulfillment);
        OnFulfillmentAdded(buildRequestFulfillment);
    }

    protected virtual void OnFulfillmentAdded(IBuildRequestFulfillment buildRequestFulfillment) {}

    public override string ToString() {
        var unitOrUpgradeName = BuildType == BuildType.Research
            ? _knowledgeBase.GetUpgradeData(UnitOrUpgradeType).Name
            : $"{QuantityFulfilled}/{QuantityRequested} {_knowledgeBase.GetUnitTypeData(UnitOrUpgradeType).Name}";

        var when = $"at {AtSupply} supply";
        if (AtSupply == 0) {
            when = "";
        }
        else if (AtSupply <= _controller.CurrentSupply) {
            when = "now";
        }

        return $"{BuildType.ToString()} {unitOrUpgradeName} {when}".TrimEnd();
    }
}
