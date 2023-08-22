using Moq;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.Builds.BuildRequests.Fulfillment;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.Tests.Fixtures;

namespace Sajuuk.Tests.Builds.BuildRequests;

public class QuantityBuildRequestTests : IClassFixture<NoLoggerFixture> {
    private readonly Mock<IController> _controllerMock = new Mock<IController>();
    private readonly Mock<IUnitsTracker> _unitsTrackerMock = new Mock<IUnitsTracker>();
    private readonly Mock<IBuildRequestFulfillmentTracker> _buildRequestFulfillmentTrackerMock = new Mock<IBuildRequestFulfillmentTracker>();
    private readonly KnowledgeBase _knowledgeBase = new TestKnowledgeBase();
    private readonly BuildRequestFactory _buildRequestFactory;

    public QuantityBuildRequestTests() {
        var inProgressFulfillments = new HashSet<IBuildRequestFulfillment>();
        _buildRequestFulfillmentTrackerMock
            .Setup(buildRequestFulfillmentTracker => buildRequestFulfillmentTracker.TrackFulfillment(It.IsAny<IBuildRequestFulfillment>()))
            .Callback<IBuildRequestFulfillment>(buildRequestFulfillment => inProgressFulfillments.Add(buildRequestFulfillment));

        _buildRequestFulfillmentTrackerMock
            .Setup(buildRequestFulfillmentTracker => buildRequestFulfillmentTracker.FulfillmentsInProgress)
            .Returns(inProgressFulfillments);

        _buildRequestFactory = new BuildRequestFactory(
            _knowledgeBase,
            _controllerMock.Object,
            _unitsTrackerMock.Object,
            _buildRequestFulfillmentTrackerMock.Object
        );
    }

    [Theory]
    [InlineData(BuildRequestFulfillmentStatus.Preparing)]
    [InlineData(BuildRequestFulfillmentStatus.Executing)]
    [InlineData(BuildRequestFulfillmentStatus.Completed)]
    public void GivenNonFailedFulfillment_WhenAddFulfillment_ThenQuantityFulfilledIsOne(BuildRequestFulfillmentStatus status) {
        // Arrange
        var quantityBuildRequest = _buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train, Units.Drone, quantity: 3);

        var fulfillmentMock = new Mock<IBuildRequestFulfillment>();
        fulfillmentMock.Setup(fulfillment => fulfillment.Status).Returns(status);
        fulfillmentMock.Setup(fulfillment => fulfillment.CanSatisfy(quantityBuildRequest)).Returns(true);

        // Act
        quantityBuildRequest.AddFulfillment(fulfillmentMock.Object);

        // Assert
        Assert.Equal(1, quantityBuildRequest.QuantityFulfilled);
    }

    [Theory]
    [InlineData(BuildRequestFulfillmentStatus.Canceled)]
    [InlineData(BuildRequestFulfillmentStatus.Prevented)]
    [InlineData(BuildRequestFulfillmentStatus.Aborted)]
    public void GivenFailedFulfillment_WhenAddFulfillment_ThenQuantityFulfilledIsZero(BuildRequestFulfillmentStatus status) {
        // Arrange
        var quantityBuildRequest = _buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train, Units.Drone, quantity: 3);

        var fulfillmentMock = new Mock<IBuildRequestFulfillment>();
        fulfillmentMock.Setup(fulfillment => fulfillment.Status).Returns(status);
        fulfillmentMock.Setup(fulfillment => fulfillment.CanSatisfy(quantityBuildRequest)).Returns(true);

        // Act
        quantityBuildRequest.AddFulfillment(fulfillmentMock.Object);

        // Assert
        Assert.Equal(0, quantityBuildRequest.QuantityFulfilled);
    }

    [Fact]
    public void GivenSatisfyingFulfillment_WhenAddFulfillmentToOtherTargetBuildRequest_ThenQuantityFulfilledIsZero() {
        // Arrange
        var quantityBuildRequest = _buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train, Units.Drone, quantity: 3);
        var otherTargetBuildRequest = _buildRequestFactory.CreateTargetBuildRequest(BuildType.Train, unitOrUpgradeType: 1, targetQuantity: 3);

        var fulfillmentMock = new Mock<IBuildRequestFulfillment>();
        fulfillmentMock.Setup(fulfillment => fulfillment.CanSatisfy(quantityBuildRequest)).Returns(true);

        // Act
        otherTargetBuildRequest.AddFulfillment(fulfillmentMock.Object);

        // Assert
        Assert.Equal(0, quantityBuildRequest.QuantityFulfilled);
    }

    [Fact]
    public void GivenSatisfyingFulfillment_WhenAddFulfillmentToOtherQuantityBuildRequest_ThenQuantityFulfilledIsZero() {
        // Arrange
        var quantityBuildRequest = _buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train, Units.Drone, quantity: 3);
        var otherQuantityBuildRequest = _buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train, unitOrUpgradeType: 1, quantity: 3);

        var fulfillmentMock = new Mock<IBuildRequestFulfillment>();
        fulfillmentMock.Setup(fulfillment => fulfillment.CanSatisfy(quantityBuildRequest)).Returns(true);

        // Act
        otherQuantityBuildRequest.AddFulfillment(fulfillmentMock.Object);

        // Assert
        Assert.Equal(0, quantityBuildRequest.QuantityFulfilled);
    }

    [Fact]
    public void GivenTrainUnitAndSatisfyingUnit_WhenGetQuantityFulfilled_ThenQuantityFulfilledIsZero() {
        // Arrange
        var quantityBuildRequest = _buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train, Units.Drone, quantity: 3);

        _unitsTrackerMock
            .Setup(unitsTracker => unitsTracker.GetUnits(It.IsAny<IEnumerable<Unit>>(), It.IsAny<uint>()))
            .Returns(new List<Unit> {
                TestUtils.CreateUnit(quantityBuildRequest.UnitOrUpgradeType, _knowledgeBase)
            });

        // Act & Assert
        Assert.Equal(0, quantityBuildRequest.QuantityFulfilled);
    }
}
