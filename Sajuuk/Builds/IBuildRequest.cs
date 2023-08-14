namespace Sajuuk.Builds;

public interface IBuildRequest : IFulfillableBuildRequest {
    public new int QuantityRequested { get; set; }
    public new uint AtSupply { get; set;  }
    public new BuildBlockCondition BlockCondition { get; set; }
    public new BuildRequestPriority Priority { get; set; }
}
