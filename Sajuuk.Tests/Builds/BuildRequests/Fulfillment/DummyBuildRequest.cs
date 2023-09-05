using Sajuuk.Builds.BuildRequests;
using Sajuuk.Builds.BuildRequests.Fulfillment;

namespace Sajuuk.Tests.Builds.BuildRequests.Fulfillment;

public class DummyBuildRequest : IBuildRequest {
    public DummyBuildRequest(BuildType buildType, uint unitOrUpgradeType) {
        BuildType = buildType;
        UnitOrUpgradeType = unitOrUpgradeType;
    }

    public BuildType BuildType { get; }
    public uint UnitOrUpgradeType { get; }
    public int QuantityRequested { get; set; }
    public uint AtSupply { get; set; }
    public bool AllowQueueing { get; }
    public BuildBlockCondition BlockCondition { get; set; }
    public BuildRequestPriority Priority { get; set; }
    public int QuantityFulfilled { get; }
    public int QuantityRemaining { get; }

    public void AddFulfillment(IBuildRequestFulfillment buildRequestFulfillment) {
        throw new NotImplementedException();
    }
}
