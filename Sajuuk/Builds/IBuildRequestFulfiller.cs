using Sajuuk.Builds.BuildRequests;

namespace Sajuuk.Builds;

public interface IBuildRequestFulfiller {
    BuildRequestResult FulfillBuildRequest(IFulfillableBuildRequest buildRequest);
}