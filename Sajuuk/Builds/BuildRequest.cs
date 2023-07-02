using Sajuuk.GameData;
using Sajuuk.GameSense;

namespace Sajuuk.Builds;

public abstract class BuildRequest {
    private readonly IController _controller;
    private readonly KnowledgeBase _knowledgeBase;

    private BuildFulfillment _buildFulfillment;
    public BuildFulfillment Fulfillment => _buildFulfillment ??= GenerateBuildFulfillment();

    protected abstract BuildFulfillment GenerateBuildFulfillment();

    public readonly BuildType BuildType;
    public readonly uint UnitOrUpgradeType;
    public uint AtSupply;
    public int Requested;
    public readonly bool Queue;
    public BuildBlockCondition BlockCondition;
    public BuildRequestPriority Priority;

    protected BuildRequest(
        IController controller,
        KnowledgeBase knowledgeBase,
        BuildType buildType,
        uint unitOrUpgradeType,
        int quantity,
        uint atSupply,
        bool queue,
        BuildBlockCondition blockCondition,
        BuildRequestPriority priority
    ) {
        _controller = controller;
        _knowledgeBase = knowledgeBase;

        BuildType = buildType;
        UnitOrUpgradeType = unitOrUpgradeType;
        AtSupply = atSupply;
        Requested = quantity;
        Queue = queue;
        BlockCondition = blockCondition;
        Priority = priority;
    }

    public override string ToString() {
        var buildStepUnitOrUpgradeName = BuildType == BuildType.Research
            ? _knowledgeBase.GetUpgradeData(UnitOrUpgradeType).Name
            : $"{Fulfillment.Fulfilled}/{Requested} {_knowledgeBase.GetUnitTypeData(UnitOrUpgradeType).Name}";

        var when = $"at {AtSupply} supply";
        if (AtSupply == 0) {
            when = "";
        }
        else if (AtSupply <= _controller.CurrentSupply) {
            when = "now";
        }

        return $"{BuildType.ToString()} {buildStepUnitOrUpgradeName} {when}";
    }
}

public class QuantityBuildRequest: BuildRequest {
    public QuantityBuildRequest(
        IController controller,
        KnowledgeBase knowledgeBase,
        BuildType buildType,
        uint unitOrUpgradeType,
        int quantity,
        uint atSupply,
        bool queue,
        BuildBlockCondition blockCondition,
        BuildRequestPriority priority
    )
        : base(controller, knowledgeBase, buildType, unitOrUpgradeType, quantity, atSupply, queue, blockCondition, priority) {}

    protected override BuildFulfillment GenerateBuildFulfillment() {
        return new QuantityFulfillment(this);
    }
}

public class TargetBuildRequest: BuildRequest {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IController _controller;

    public TargetBuildRequest(
        IUnitsTracker unitsTracker,
        IController controller,
        KnowledgeBase knowledgeBase,
        BuildType buildType,
        uint unitOrUpgradeType,
        int targetQuantity,
        uint atSupply,
        bool queue,
        BuildBlockCondition blockCondition,
        BuildRequestPriority priority
    )
        : base(controller, knowledgeBase, buildType, unitOrUpgradeType, targetQuantity, atSupply, queue, blockCondition, priority) {
        _unitsTracker = unitsTracker;
        _controller = controller;
    }

    protected override BuildFulfillment GenerateBuildFulfillment() {
        return new TargetFulfillment(_unitsTracker, _controller, this);
    }
}
