namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public interface IBuildRequestFulfillmentTracker {
    /// <summary>
    /// Registers a build fulfillment so that it can be updated over time and accessed by other systems.
    /// </summary>
    /// <param name="buildRequestFulfillment">The fulfillment to keep track of.</param>
    void RegisterBuildFulfillment(IBuildRequestFulfillment buildRequestFulfillment);

    // TODO GD Add some query mechanism
}
