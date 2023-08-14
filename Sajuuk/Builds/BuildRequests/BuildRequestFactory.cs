using Sajuuk.GameSense;

namespace Sajuuk.Builds.BuildRequests;

public class BuildRequestFactory : IBuildRequestFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IController _controller;

    public BuildRequestFactory(IUnitsTracker unitsTracker, IController controller) {
        _unitsTracker = unitsTracker;
        _controller = controller;
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
        return new TargetBuildRequest(_unitsTracker, _controller, buildType, unitOrUpgradeType, targetQuantity, atSupply, queue, blockCondition, priority);
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
        return new QuantityBuildRequest(buildType, unitOrUpgradeType, quantity, atSupply, queue, blockCondition, priority);
    }
}
