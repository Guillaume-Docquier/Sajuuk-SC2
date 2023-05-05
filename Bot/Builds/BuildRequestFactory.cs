using Bot.GameSense;

namespace Bot.Builds;

public class BuildRequestFactory : IBuildRequestFactory {
    private readonly IUnitsTracker _unitsTracker;

    public BuildRequestFactory(IUnitsTracker unitsTracker) {
        _unitsTracker = unitsTracker;
    }

    public TargetBuildRequest CreateTargetBuildRequest(
        BuildType buildType,
        uint unitOrUpgradeType,
        int targetQuantity,
        uint atSupply = 0,
        bool queue = false,
        BuildBlockCondition blockCondition = BuildBlockCondition.None,
        BuildRequestPriority priority = BuildRequestPriority.Normal
    ) {
        return new TargetBuildRequest(_unitsTracker, buildType, unitOrUpgradeType, targetQuantity, atSupply, queue, blockCondition, priority);
    }

    public QuantityBuildRequest CreateQuantityBuildRequest(
        BuildType buildType,
        uint unitOrUpgradeType,
        int quantity = 1,
        uint atSupply = 0,
        bool queue = false,
        BuildBlockCondition blockCondition = BuildBlockCondition.None,
        BuildRequestPriority priority = BuildRequestPriority.Normal
    ) {
        return new QuantityBuildRequest(_unitsTracker, buildType, unitOrUpgradeType, quantity, atSupply, queue, blockCondition, priority);
    }
}
