using System.Collections.Generic;

namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public interface IBuildRequestFulfillmentTracker {
    /// <summary>
    /// Gets all fulfillments in progress that are being tracked.
    /// </summary>
    IEnumerable<IBuildRequestFulfillment> FulfillmentsInProgress { get; }

    /// <summary>
    /// Tracks a build fulfillment so that it can be updated over time and accessed by other systems.
    /// </summary>
    /// <param name="buildRequestFulfillment">The fulfillment to keep track of.</param>
    void TrackFulfillment(IBuildRequestFulfillment buildRequestFulfillment);
}
