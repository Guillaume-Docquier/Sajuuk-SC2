﻿using Bot.GameData;

namespace Bot.Builds;

public abstract class BuildRequest {
    private BuildFulfillment _buildFulfillment;
    public BuildFulfillment Fulfillment => _buildFulfillment ??= GenerateBuildFulfillment();

    protected abstract BuildFulfillment GenerateBuildFulfillment();

    public readonly BuildType BuildType;

    public readonly uint AtSupply;

    public readonly uint UnitOrUpgradeType;

    public int Requested;

    public BuildRequest(BuildType buildType, uint unitOrUpgradeType, int quantity, uint atSupply) {
        BuildType = buildType;
        UnitOrUpgradeType = unitOrUpgradeType;
        AtSupply = atSupply;
        Requested = quantity;
    }
}

public class QuantityBuildRequest: BuildRequest {
    public QuantityBuildRequest(BuildType buildType, uint unitOrUpgradeType, int quantity = 1, uint atSupply = 0)
        : base(buildType, unitOrUpgradeType, quantity, atSupply) {
    }

    protected override BuildFulfillment GenerateBuildFulfillment() {
        return new QuantityFulfillment(this);
    }

    public override string ToString() {
        var buildStepUnitOrUpgradeName = BuildType == BuildType.Research
            ? KnowledgeBase.GetUpgradeData(UnitOrUpgradeType).Name
            : $"{Fulfillment.Remaining} {KnowledgeBase.GetUnitTypeData(UnitOrUpgradeType).Name}";

        var when = AtSupply > Controller.CurrentSupply ? $"at {AtSupply} supply" : "";

        return $"{BuildType.ToString()} {buildStepUnitOrUpgradeName} {when}";
    }
}

public class TargetBuildRequest: BuildRequest {
    public TargetBuildRequest(BuildType buildType, uint unitOrUpgradeType, int targetQuantity, uint atSupply = 0)
        : base(buildType, unitOrUpgradeType, targetQuantity, atSupply) {
    }

    protected override BuildFulfillment GenerateBuildFulfillment() {
        return new TargetFulfillment(this);
    }

    public override string ToString() {
        var buildStepUnitOrUpgradeName = BuildType == BuildType.Research
            ? KnowledgeBase.GetUpgradeData(UnitOrUpgradeType).Name
            : $"{Fulfillment.Fulfilled}/{Requested} {KnowledgeBase.GetUnitTypeData(UnitOrUpgradeType).Name}";

        var when = AtSupply > Controller.CurrentSupply ? $"at {AtSupply} supply" : "";

        return $"{BuildType.ToString()} {buildStepUnitOrUpgradeName} {when}";
    }
}