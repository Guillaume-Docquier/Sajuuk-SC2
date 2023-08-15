namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public interface IBuildRequestFulfiller {
    /// <summary>
    /// Attempts to fulfill 1 quantity of the given build request.
    /// </summary>
    /// <param name="buildRequest">The build request to fulfill.</param>
    /// <returns>A BuildRequestResult that describes if the build request could be fulfilled, or why not.</returns>
    BuildRequestResult FulfillBuildRequest(IFulfillableBuildRequest buildRequest);
}
