using Bot.GameData;
using Bot.GameSense;

namespace Bot.Builds;

public class BuildRequestFactory : IBuildRequestFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IController _controller;
    private readonly KnowledgeBase _knowledgeBase;

    public BuildRequestFactory(IUnitsTracker unitsTracker, IController controller, KnowledgeBase knowledgeBase) {
        _unitsTracker = unitsTracker;
        _controller = controller;
        _knowledgeBase = knowledgeBase;
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
        return new TargetBuildRequest(_unitsTracker, _controller, _knowledgeBase, buildType, unitOrUpgradeType, targetQuantity, atSupply, queue, blockCondition, priority);
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
        return new QuantityBuildRequest(_controller, _knowledgeBase, buildType, unitOrUpgradeType, quantity, atSupply, queue, blockCondition, priority);
    }
}
