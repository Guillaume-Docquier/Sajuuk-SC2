namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public interface IBuildRequestFulfillment {
    /// <summary>
    /// The current status of the fulfillment.
    /// </summary>
    BuildRequestFulfillmentStatus Status { get; }

    /// <summary>
    /// The expected frame at which this fulfillment should complete.
    /// </summary>
    uint ExpectedCompletionFrame { get; }

    /// <summary>
    /// Updates the fulfillment status.
    /// </summary>
    void UpdateStatus();

    /// <summary>
    /// Aborts this fulfillment.
    /// </summary>
    void Abort();

    /// <summary>
    /// Cancels this fulfillment, if possible.
    /// </summary>
    void Cancel();

    /// <summary>
    /// Determines whether this fulfillment could fulfill the given build request
    /// </summary>
    /// <param name="buildRequest">The build request to validate against.</param>
    /// <returns>True if this fulfillment could satisfy the given build request.</returns>
    bool CanSatisfy(IBuildRequest buildRequest);
}
