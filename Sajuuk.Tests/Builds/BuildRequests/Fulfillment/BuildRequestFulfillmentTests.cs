using Sajuuk.Builds.BuildRequests;
using Sajuuk.Builds.BuildRequests.Fulfillment;
using Sajuuk.Tests.Fixtures;

namespace Sajuuk.Tests.Builds.BuildRequests.Fulfillment;

public class BuildRequestFulfillmentTests : IClassFixture<NoLoggerFixture> {
    private readonly DummyBuildRequestFulfillment _buildRequestFulfillment = new DummyBuildRequestFulfillment();

    [Fact]
    public void GivenNothing_WhenAbort_ThenStatusIsAborted() {
        // Act
        _buildRequestFulfillment.Abort();

        // Assert
        Assert.Equal(BuildRequestFulfillmentStatus.Aborted, _buildRequestFulfillment.Status);
    }

    private class DummyBuildRequestFulfillment : BuildRequestFulfillment {
        public override ulong ExpectedCompletionFrame { get; }

        public override void UpdateStatus() {
            throw new NotImplementedException();
        }

        public override bool CanSatisfy(IBuildRequest buildRequest) {
            throw new NotImplementedException();
        }
    }
}
