using Bot.GameData;

namespace Bot.Builds;

public abstract class BuildRequest {
    private BuildFulfillment _buildFulfillment;
    public BuildFulfillment Fulfillment => _buildFulfillment ??= GenerateBuildFulfillment();

    protected abstract BuildFulfillment GenerateBuildFulfillment();

    public readonly BuildType BuildType;

    public readonly uint AtSupply;

    public readonly uint UnitOrUpgradeType;

    public int Requested;

    public readonly bool Queue;

    public BuildRequestPriority Priority = BuildRequestPriority.Normal;

    protected BuildRequest(BuildType buildType, uint unitOrUpgradeType, int quantity, uint atSupply, bool queue) {
        BuildType = buildType;
        UnitOrUpgradeType = unitOrUpgradeType;
        AtSupply = atSupply;
        Requested = quantity;
        Queue = queue;
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
    public QuantityBuildRequest(BuildType buildType, uint unitOrUpgradeType, int quantity = 1, uint atSupply = 0, bool queue = false)
        : base(buildType, unitOrUpgradeType, quantity, atSupply, queue) {
    }

    protected override BuildFulfillment GenerateBuildFulfillment() {
        return new QuantityFulfillment(this);
    }
}

public class TargetBuildRequest: BuildRequest {
    public TargetBuildRequest(BuildType buildType, uint unitOrUpgradeType, int targetQuantity, uint atSupply = 0, bool queue = false)
        : base(buildType, unitOrUpgradeType, targetQuantity, atSupply, queue) {
    }

    protected override BuildFulfillment GenerateBuildFulfillment() {
        return new TargetFulfillment(this);
    }
}
