using Bot.GameData;

namespace Bot.Builds;

public abstract class BuildRequest {
    private BuildFulfillment _buildFulfillment;
    public BuildFulfillment Fulfillment => _buildFulfillment ??= GenerateBuildFulfillment();

    protected abstract BuildFulfillment GenerateBuildFulfillment();

    public readonly BuildType BuildType;
    public readonly uint UnitOrUpgradeType;
    public readonly uint AtSupply;
    public int Requested;
    public readonly bool Queue;
    public bool IsBlocking;
    public BuildRequestPriority Priority;

    protected BuildRequest(BuildType buildType, uint unitOrUpgradeType, int quantity, uint atSupply, bool queue, bool isBlocking, BuildRequestPriority priority) {
        BuildType = buildType;
        UnitOrUpgradeType = unitOrUpgradeType;
        AtSupply = atSupply;
        Requested = quantity;
        Queue = queue;
        IsBlocking = isBlocking;
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
    public QuantityBuildRequest(
        BuildType buildType,
        uint unitOrUpgradeType,
        int quantity = 1,
        uint atSupply = 0,
        bool queue = false,
        bool isBlocking = false,
        BuildRequestPriority priority = BuildRequestPriority.Normal
    )
        : base(buildType, unitOrUpgradeType, quantity, atSupply, queue, isBlocking, priority) {}

    protected override BuildFulfillment GenerateBuildFulfillment() {
        return new QuantityFulfillment(this);
    }
}

public class TargetBuildRequest: BuildRequest {
    public TargetBuildRequest(
        BuildType buildType,
        uint unitOrUpgradeType,
        int targetQuantity,
        uint atSupply = 0,
        bool queue = false,
        bool isBlocking = false,
        BuildRequestPriority priority = BuildRequestPriority.Normal
    )
        : base(buildType, unitOrUpgradeType, targetQuantity, atSupply, queue, isBlocking, priority) {}

    protected override BuildFulfillment GenerateBuildFulfillment() {
        return new TargetFulfillment(this);
    }
}
