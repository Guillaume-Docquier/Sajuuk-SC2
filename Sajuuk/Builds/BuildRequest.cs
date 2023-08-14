namespace Sajuuk.Builds;

public abstract class BuildRequest : IBuildRequest {
    public BuildType BuildType { get; }
    public uint UnitOrUpgradeType { get; }
    public int QuantityRequested { get; set; }
    public uint AtSupply { get; set; }
    public bool AllowQueueing { get; }
    public BuildBlockCondition BlockCondition { get; set; }
    public BuildRequestPriority Priority { get; set; }

    protected BuildRequest(
        BuildType buildType,
        uint unitOrUpgradeType,
        int quantity,
        uint atSupply,
        bool allowQueueing,
        BuildBlockCondition blockCondition,
        BuildRequestPriority priority
    ) {
        BuildType = buildType;
        UnitOrUpgradeType = unitOrUpgradeType;
        AtSupply = atSupply;
        QuantityRequested = quantity;
        AllowQueueing = allowQueueing;
        BlockCondition = blockCondition;
        Priority = priority;
    }

    public abstract int QuantityFulfilled { get; }
    public abstract int QuantityRemaining { get; }

    public abstract void Fulfill(int quantity);
}
