namespace Sajuuk.Builds.BuildRequests;

public interface IFulfillableBuildRequest {
    public BuildType BuildType { get; }
    public uint UnitOrUpgradeType { get; }
    public int QuantityRequested { get; }
    public uint AtSupply { get; }
    public bool AllowQueueing { get; }
    public BuildBlockCondition BlockCondition { get; }
    public BuildRequestPriority Priority { get; }

    public int QuantityFulfilled { get; }
    public int QuantityRemaining { get; }

    public void Fulfill(int quantity);
}
