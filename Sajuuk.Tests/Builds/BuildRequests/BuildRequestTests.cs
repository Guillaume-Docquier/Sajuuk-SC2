using Moq;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.Builds.BuildRequests.Fulfillment;
using Sajuuk.GameData;
using Sajuuk.Tests.Fixtures;

namespace Sajuuk.Tests.Builds.BuildRequests;

public class BuildRequestTests : IClassFixture<NoLoggerFixture> {
    private readonly Mock<IController> _controllerMock = new Mock<IController>();
    private readonly Mock<IBuildRequestFulfillmentTracker> _buildRequestFulfillmentTrackerMock = new Mock<IBuildRequestFulfillmentTracker>();
    private readonly KnowledgeBase _knowledgeBase = new TestKnowledgeBase();

    [Fact]
    public void GivenFulfillment_WhenAddFulfillment_ThenTracksFulfillmentUsingTracker() {
        // Arrange
        var buildRequest = CreateBuildRequest();

        // Act
        buildRequest.AddFulfillment(Mock.Of<IBuildRequestFulfillment>());

        // Assert
        _buildRequestFulfillmentTrackerMock
            .Verify(buildRequestFulfillmentTracker => buildRequestFulfillmentTracker.TrackFulfillment(It.IsAny<IBuildRequestFulfillment>()), Times.Once);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 3, 0)]
    [InlineData(3, 1, 2)]
    public void GivenQuantityRequestedAndFulfilled_WhenGetQuantityRemaining_ThenReturnsTheDifferenceWithAMinimumOfZero(int quantityRequested, int quantityFulfilled, int expectedQuantityRemaining) {
        // Arrange
        var buildRequest = CreateBuildRequest(quantityRequested, quantityFulfilled);

        // Act & Assert
        Assert.Equal(expectedQuantityRemaining, buildRequest.QuantityRemaining);
    }

    private BuildRequest CreateBuildRequest(int quantityRequested = 1, int quantityFulfilled = 0) {
        return new DummyBuildRequest(
            _knowledgeBase,
            _controllerMock.Object,
            _buildRequestFulfillmentTrackerMock.Object,
            BuildType.Build,
            Units.Drone,
            quantityRequested,
            atSupply: 0,
            allowQueueing: false,
            BuildBlockCondition.None,
            BuildRequestPriority.Normal,
            quantityFulfilled
        );
    }

    private class DummyBuildRequest : BuildRequest {
        public DummyBuildRequest(
            KnowledgeBase knowledgeBase,
            IController controller,
            IBuildRequestFulfillmentTracker buildRequestFulfillmentTracker,
            BuildType buildType,
            uint unitOrUpgradeType,
            int quantity,
            uint atSupply,
            bool allowQueueing,
            BuildBlockCondition blockCondition,
            BuildRequestPriority priority,
            int quantityFulfilled
        ) : base(knowledgeBase, controller, buildRequestFulfillmentTracker, buildType, unitOrUpgradeType, quantity, atSupply, allowQueueing, blockCondition, priority) {
            QuantityFulfilled = quantityFulfilled;
        }

        public override int QuantityFulfilled { get; }
    }
}
