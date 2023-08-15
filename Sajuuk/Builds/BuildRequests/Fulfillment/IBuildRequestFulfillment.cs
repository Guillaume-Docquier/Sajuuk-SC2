namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public interface IBuildRequestFulfillment {
    public BuildRequestFulfillmentStatus Status { get; }

    public void UpdateStatus();
}
