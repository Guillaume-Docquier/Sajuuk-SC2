namespace Sajuuk.Builds.BuildRequests;

public class QuantityBuildRequest : BuildRequest {
    private int _quantityFulfilled = 0;
    public override int QuantityFulfilled => _quantityFulfilled;
    public override int QuantityRemaining => QuantityRequested - QuantityFulfilled;

    public QuantityBuildRequest(
        BuildType buildType,
        uint unitOrUpgradeType,
        int quantity,
        uint atSupply,
        bool queue,
        BuildBlockCondition blockCondition,
        BuildRequestPriority priority
    )
        : base(buildType, unitOrUpgradeType, quantity, atSupply, queue, blockCondition, priority) {}

    public override void Fulfill(int quantity) {
        _quantityFulfilled += quantity;
    }
}