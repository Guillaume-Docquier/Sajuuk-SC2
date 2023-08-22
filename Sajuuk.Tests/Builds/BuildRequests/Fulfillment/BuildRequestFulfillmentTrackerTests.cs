using Moq;
using Sajuuk.Builds.BuildRequests.Fulfillment;
using Sajuuk.Tests.Fixtures;

namespace Sajuuk.Tests.Builds.BuildRequests.Fulfillment;

public class BuildRequestFulfillmentTrackerTests : IClassFixture<NoLoggerFixture> {
    private readonly BuildRequestFulfillmentTracker _buildRequestFulfillmentTracker = new BuildRequestFulfillmentTracker();

    [Theory]
    [InlineData(BuildRequestFulfillmentStatus.None)]
    [InlineData(BuildRequestFulfillmentStatus.Preparing)]
    [InlineData(BuildRequestFulfillmentStatus.Executing)]
    public void GivenNonTerminatedFulfillment_WhenTrackFulfillment_ThenAddsToFulfillmentsInProgress(BuildRequestFulfillmentStatus status) {
        // Arrange
        var fulfillmentMock = new Mock<IBuildRequestFulfillment>();
        fulfillmentMock.Setup(fulfillment => fulfillment.Status).Returns(status);

        // Act
        _buildRequestFulfillmentTracker.TrackFulfillment(fulfillmentMock.Object);

        // Assert
        var trackedFulfillment = Assert.Single(_buildRequestFulfillmentTracker.FulfillmentsInProgress);
        Assert.Equal(fulfillmentMock.Object, trackedFulfillment);
    }

    [Theory]
    [InlineData(BuildRequestFulfillmentStatus.Completed)]
    [InlineData(BuildRequestFulfillmentStatus.Canceled)]
    [InlineData(BuildRequestFulfillmentStatus.Aborted)]
    [InlineData(BuildRequestFulfillmentStatus.Prevented)]
    public void GivenTerminatedFulfillment_WhenTrackFulfillment_ThenDoesNotAddToFulfillmentsInProgress(BuildRequestFulfillmentStatus status) {
        // Arrange
        var fulfillmentMock = new Mock<IBuildRequestFulfillment>();
        fulfillmentMock.Setup(fulfillment => fulfillment.Status).Returns(status);

        // Act
        _buildRequestFulfillmentTracker.TrackFulfillment(fulfillmentMock.Object);

        // Assert
        Assert.Empty(_buildRequestFulfillmentTracker.FulfillmentsInProgress);
    }

    [Theory]
    [InlineData(BuildRequestFulfillmentStatus.None)]
    [InlineData(BuildRequestFulfillmentStatus.Preparing)]
    [InlineData(BuildRequestFulfillmentStatus.Executing)]
    public void GivenNonTerminatedTrackedFulfillment_WhenUpdate_ThenUpdatesFulfillment(BuildRequestFulfillmentStatus status) {
        // Arrange
        var fulfillmentMock = new Mock<IBuildRequestFulfillment>();
        fulfillmentMock.Setup(fulfillment => fulfillment.Status).Returns(status);
        _buildRequestFulfillmentTracker.TrackFulfillment(fulfillmentMock.Object);

        // Act
        _buildRequestFulfillmentTracker.Update(null, null);

        // Assert
        fulfillmentMock.Verify(fulfillment => fulfillment.UpdateStatus(), Times.Once);
    }

    [Theory]
    [InlineData(BuildRequestFulfillmentStatus.Completed)]
    [InlineData(BuildRequestFulfillmentStatus.Canceled)]
    [InlineData(BuildRequestFulfillmentStatus.Aborted)]
    [InlineData(BuildRequestFulfillmentStatus.Prevented)]
    public void GivenTerminatedTrackedFulfillment_WhenUpdate_ThenDoesNotUpdateFulfillment(BuildRequestFulfillmentStatus status) {
        // Arrange
        var fulfillmentMock = new Mock<IBuildRequestFulfillment>();
        fulfillmentMock.Setup(fulfillment => fulfillment.Status).Returns(status);
        _buildRequestFulfillmentTracker.TrackFulfillment(fulfillmentMock.Object);

        // Act
        _buildRequestFulfillmentTracker.Update(null, null);

        // Assert
        fulfillmentMock.Verify(fulfillment => fulfillment.UpdateStatus(), Times.Never);
    }

    [Theory]
    [InlineData(BuildRequestFulfillmentStatus.None)]
    [InlineData(BuildRequestFulfillmentStatus.Preparing)]
    [InlineData(BuildRequestFulfillmentStatus.Executing)]
    public void GivenNonTerminatedTrackedFulfillment_WhenFulfillmentTerminatesAfterUpdate_ThenRemovesFulfillmentFromFulfillmentsInProgress(BuildRequestFulfillmentStatus status) {
        // Arrange
        var fulfillmentMock = new Mock<IBuildRequestFulfillment>();
        fulfillmentMock.Setup(fulfillment => fulfillment.Status).Returns(status);
        fulfillmentMock
            .Setup(fulfillment => fulfillment.UpdateStatus())
            .Callback(() => fulfillmentMock.Setup(fulfillment => fulfillment.Status).Returns(BuildRequestFulfillmentStatus.Terminated));

        _buildRequestFulfillmentTracker.TrackFulfillment(fulfillmentMock.Object);

        // Act
        _buildRequestFulfillmentTracker.Update(null, null);

        // Assert
        Assert.Empty(_buildRequestFulfillmentTracker.FulfillmentsInProgress);
    }
}
