using Moq;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.Builds.BuildRequests.Fulfillment;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.Managers;
using Sajuuk.Tests.Fixtures;

namespace Sajuuk.Tests.Builds.BuildRequests;

public class TargetBuildRequestTests : IClassFixture<NoLoggerFixture> {
    private readonly Mock<IController> _controllerMock = new Mock<IController>();
    private readonly Mock<IUnitsTracker> _unitsTrackerMock = new Mock<IUnitsTracker>();
    private readonly Mock<IBuildRequestFulfillmentTracker> _buildRequestFulfillmentTrackerMock = new Mock<IBuildRequestFulfillmentTracker>();
    private readonly KnowledgeBase _knowledgeBase = new TestKnowledgeBase();
    private readonly BuildRequestFactory _buildRequestFactory;

    public TargetBuildRequestTests() {
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

    [Fact]
    public void GivenSatisfyingFulfillment_WhenAddFulfillment_ThenQuantityFulfilledUpdates() {
        // Arrange
        var targetBuildRequest = _buildRequestFactory.CreateTargetBuildRequest(BuildType.Train, Units.Drone, targetQuantity: 3);

        var fulfillmentMock = new Mock<IBuildRequestFulfillment>();
        fulfillmentMock.Setup(fulfillment => fulfillment.CanSatisfy(targetBuildRequest)).Returns(true);

        // Act
        targetBuildRequest.AddFulfillment(fulfillmentMock.Object);

        // Assert
        Assert.Equal(1, targetBuildRequest.QuantityFulfilled);
    }

    [Fact]
    public void GivenSatisfyingFulfillment_WhenAddFulfillmentToOtherTargetBuildRequest_ThenQuantityFulfilledUpdates() {
        // Arrange
        var targetBuildRequest = _buildRequestFactory.CreateTargetBuildRequest(BuildType.Train, Units.Drone, targetQuantity: 3);
        var otherTargetBuildRequest = _buildRequestFactory.CreateTargetBuildRequest(BuildType.Train, unitOrUpgradeType: 1, targetQuantity: 3);

        var fulfillmentMock = new Mock<IBuildRequestFulfillment>();
        fulfillmentMock.Setup(fulfillment => fulfillment.CanSatisfy(targetBuildRequest)).Returns(true);

        // Act
        otherTargetBuildRequest.AddFulfillment(fulfillmentMock.Object);

        // Assert
        Assert.Equal(1, targetBuildRequest.QuantityFulfilled);
    }

    [Fact]
    public void GivenSatisfyingFulfillment_WhenAddFulfillmentToOtherQuantityBuildRequest_ThenQuantityFulfilledUpdates() {
        // Arrange
        var targetBuildRequest = _buildRequestFactory.CreateTargetBuildRequest(BuildType.Train, Units.Drone, targetQuantity: 3);
        var otherQuantityBuildRequest = _buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train, unitOrUpgradeType: 1, quantity: 3);

        var fulfillmentMock = new Mock<IBuildRequestFulfillment>();
        fulfillmentMock.Setup(fulfillment => fulfillment.CanSatisfy(targetBuildRequest)).Returns(true);

        // Act
        otherQuantityBuildRequest.AddFulfillment(fulfillmentMock.Object);

        // Assert
        Assert.Equal(1, targetBuildRequest.QuantityFulfilled);
    }

    [Fact]
    public void GivenTrainUnitAndSatisfyingUnit_WhenGetQuantityFulfilled_ThenQuantityFulfilledIsOne() {
        // Arrange
        var targetBuildRequest = _buildRequestFactory.CreateTargetBuildRequest(BuildType.Train, Units.Drone, targetQuantity: 3);

        _unitsTrackerMock
            .Setup(unitsTracker => unitsTracker.GetUnits(It.IsAny<IEnumerable<Unit>>(), It.IsAny<uint>()))
            .Returns(new List<Unit> {
                TestUtils.CreateUnit(targetBuildRequest.UnitOrUpgradeType, _knowledgeBase)
            });

        // Act & Assert
        Assert.Equal(1, targetBuildRequest.QuantityFulfilled);
    }

    [Fact]
    public void GivenTrainUnitAndSatisfyingUnitAndSatisfyingFulfillment_WhenGetQuantityFulfilled_ThenQuantityFulfilledIsTwo() {
        // Arrange
        var targetBuildRequest = _buildRequestFactory.CreateTargetBuildRequest(BuildType.Train, Units.Drone, targetQuantity: 3);

        _unitsTrackerMock
            .Setup(unitsTracker => unitsTracker.GetUnits(It.IsAny<IEnumerable<Unit>>(), It.IsAny<uint>()))
            .Returns(new List<Unit> {
                TestUtils.CreateUnit(targetBuildRequest.UnitOrUpgradeType, _knowledgeBase)
            });

        var fulfillmentMock = new Mock<IBuildRequestFulfillment>();
        fulfillmentMock.Setup(fulfillment => fulfillment.CanSatisfy(targetBuildRequest)).Returns(true);

        targetBuildRequest.AddFulfillment(fulfillmentMock.Object);

        // Act & Assert
        Assert.Equal(2, targetBuildRequest.QuantityFulfilled);
    }

    [Fact]
    public void GivenResearchUpgradeAndUpgradeIsResearched_WhenGetQuantityFulfilled_ThenQuantityFulfilledIsOne() {
        // Arrange
        var targetBuildRequest = _buildRequestFactory.CreateTargetBuildRequest(BuildType.Research, Upgrades.Burrow, targetQuantity: 3);

        _controllerMock
            .Setup(controller => controller.ResearchedUpgrades)
            .Returns(new HashSet<uint> {
                Upgrades.Burrow
            });

        // Act & Assert
        Assert.Equal(1, targetBuildRequest.QuantityFulfilled);
    }

    [Fact]
    public void GivenBuildExtractorAndSupervisedExtractor_WhenGetQuantityFulfilled_ThenQuantityFulfilledIsOne() {
        // Arrange
        var targetBuildRequest = _buildRequestFactory.CreateTargetBuildRequest(BuildType.Build, Units.Extractor, targetQuantity: 3);

        var extractor = TestUtils.CreateUnit(targetBuildRequest.UnitOrUpgradeType, _knowledgeBase);
        extractor.Supervisor = Mock.Of<Supervisor>();

        _unitsTrackerMock
            .Setup(unitsTracker => unitsTracker.GetUnits(It.IsAny<IEnumerable<Unit>>(), It.IsAny<uint>()))
            .Returns(new List<Unit> {
                extractor
            });

        // Act & Assert
        Assert.Equal(1, targetBuildRequest.QuantityFulfilled);
    }

    [Fact]
    public void GivenBuildExtractorAndUnsupervisedExtractor_WhenGetQuantityFulfilled_ThenQuantityFulfilledIsZero() {
        // Arrange
        var targetBuildRequest = _buildRequestFactory.CreateTargetBuildRequest(BuildType.Build, Units.Extractor, targetQuantity: 3);

        _unitsTrackerMock
            .Setup(unitsTracker => unitsTracker.GetUnits(It.IsAny<IEnumerable<Unit>>(), It.IsAny<uint>()))
            .Returns(new List<Unit> {
                TestUtils.CreateUnit(targetBuildRequest.UnitOrUpgradeType, _knowledgeBase)
            });

        // Act & Assert
        Assert.Equal(0, targetBuildRequest.QuantityFulfilled);
    }
}
