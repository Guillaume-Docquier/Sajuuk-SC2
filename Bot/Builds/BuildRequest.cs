using Bot.GameData;
using Bot.GameSense;

namespace Bot.Builds;

public abstract class BuildRequest {
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
        BuildType buildType,
        uint unitOrUpgradeType,
        int quantity,
        uint atSupply,
        bool queue,
        BuildBlockCondition blockCondition,
        BuildRequestPriority priority
    ) {
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
            ? KnowledgeBase.GetUpgradeData(UnitOrUpgradeType).Name
            : $"{Fulfillment.Fulfilled}/{Requested} {KnowledgeBase.GetUnitTypeData(UnitOrUpgradeType).Name}";

        var when = $"at {AtSupply} supply";
        if (AtSupply == 0) {
            when = "";
        }
        else if (AtSupply <= Controller.CurrentSupply) {
            when = "now";
        }

        return $"{BuildType.ToString()} {buildStepUnitOrUpgradeName} {when}";
    }
}

public class QuantityBuildRequest: BuildRequest {
    private readonly IUnitsTracker _unitsTracker;

    public QuantityBuildRequest(
        IUnitsTracker unitsTracker,
        BuildType buildType,
        uint unitOrUpgradeType,
        int quantity,
        uint atSupply,
        bool queue,
        BuildBlockCondition blockCondition,
        BuildRequestPriority priority
    )
        : base(buildType, unitOrUpgradeType, quantity, atSupply, queue, blockCondition, priority) {
        _unitsTracker = unitsTracker;
    }

    protected override BuildFulfillment GenerateBuildFulfillment() {
        return new QuantityFulfillment(this, _unitsTracker);
    }
}

public class TargetBuildRequest: BuildRequest {
    private readonly IUnitsTracker _unitsTracker;

    public TargetBuildRequest(
        IUnitsTracker unitsTracker,
        BuildType buildType,
        uint unitOrUpgradeType,
        int targetQuantity,
        uint atSupply,
        bool queue,
        BuildBlockCondition blockCondition,
        BuildRequestPriority priority
    )
        : base(buildType, unitOrUpgradeType, targetQuantity, atSupply, queue, blockCondition, priority) {
        _unitsTracker = unitsTracker;
    }

    protected override BuildFulfillment GenerateBuildFulfillment() {
        return new TargetFulfillment(this, _unitsTracker);
    }
}
