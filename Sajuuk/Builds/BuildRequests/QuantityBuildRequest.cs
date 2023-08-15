using Sajuuk.GameData;

namespace Sajuuk.Builds.BuildRequests;

public class QuantityBuildRequest : BuildRequest {
    private int _quantityFulfilled = 0;
    public override int QuantityFulfilled => _quantityFulfilled;
    public override int QuantityRemaining => QuantityRequested - QuantityFulfilled;

    public QuantityBuildRequest(
        KnowledgeBase knowledgeBase,
        IController controller,
        BuildType buildType,
        uint unitOrUpgradeType,
        int quantity,
        uint atSupply,
        bool queue,
        BuildBlockCondition blockCondition,
        BuildRequestPriority priority
    )
        : base(knowledgeBase, controller, buildType, unitOrUpgradeType, quantity, atSupply, queue, blockCondition, priority) {}

    public override void Fulfill(int quantity) {
        _quantityFulfilled += quantity;
    }
}
