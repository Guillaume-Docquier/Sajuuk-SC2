using Moq;
using Sajuuk.Actions;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.Builds.BuildRequests.Fulfillment;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis;
using Sajuuk.Tests.Fixtures;

namespace Sajuuk.Tests.Builds.BuildRequests.Fulfillment;

public class TrainUnitFulfillmentTests : IClassFixture<NoLoggerFixture> {
    private readonly Mock<IUnitsTracker> _unitsTrackerMock = new Mock<IUnitsTracker>();
    private readonly Mock<IFrameClock> _frameClockMock = new Mock<IFrameClock>();
    private readonly KnowledgeBase _knowledgeBase = new TestKnowledgeBase();
    private readonly Mock<IPathfinder> _pathfinderMock = new Mock<IPathfinder>();
    private readonly Mock<ITerrainTracker> _terrainTrackerMock = new Mock<ITerrainTracker>();
    private readonly Mock<IController> _controllerMock = new Mock<IController>();
    private readonly IActionBuilder _actionBuilder;

    private readonly BuildRequestFulfillmentFactory _buildRequestFulfillmentFactory;

    public TrainUnitFulfillmentTests() {
        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns(0);
        _actionBuilder = new ActionBuilder(_knowledgeBase);

        _buildRequestFulfillmentFactory = new BuildRequestFulfillmentFactory(
            _unitsTrackerMock.Object,
            _frameClockMock.Object,
            _knowledgeBase,
            _pathfinderMock.Object,
            new FootprintCalculator(_terrainTrackerMock.Object),
            _terrainTrackerMock.Object,
            _controllerMock.Object
        );
    }

    private static IEnumerable<object[]> BuildRequestsToSatisfy() {
        yield return new object[] { new DummyBuildRequest(BuildType.Train, Units.Drone)      , true };
        yield return new object[] { new DummyBuildRequest(BuildType.Build, Units.Drone)      , false };
        yield return new object[] { new DummyBuildRequest(BuildType.Research, Units.Drone)   , false };
        yield return new object[] { new DummyBuildRequest(BuildType.Expand, Units.Drone)     , false };
        yield return new object[] { new DummyBuildRequest(BuildType.Train, Units.Zergling)   , false };
        yield return new object[] { new DummyBuildRequest(BuildType.Build, Units.Zergling)   , false };
        yield return new object[] { new DummyBuildRequest(BuildType.Research, Units.Zergling), false };
        yield return new object[] { new DummyBuildRequest(BuildType.Expand, Units.Zergling)  , false };
    }

    [Theory]
    [MemberData(nameof(BuildRequestsToSatisfy))]
    public void GivenBuildRequest_WhenCanSatisfy_ThenReturnsTrueIfBuildTypeIsTrainAndUnitTypeMatches(IBuildRequest buildRequest, bool expectedCanSatisfy) {
        // Arrange
        var producer = TestUtils.CreateUnit(Units.Larva, _knowledgeBase, actionBuilder: _actionBuilder);
        const uint unitTypeToTrain = Units.Drone;
        var producerOrder = producer.TrainUnit(unitTypeToTrain);
        var trainUnitFulfillment = _buildRequestFulfillmentFactory.CreateTrainUnitFulfillment(producer, producerOrder, unitTypeToTrain);

        // Act
        var canSatisfy = trainUnitFulfillment.CanSatisfy(buildRequest);

        // Assert
        Assert.Equal(expectedCanSatisfy, canSatisfy);
    }

    private static IEnumerable<object[]> ProducerTypes() {
        yield return new object[] { Units.Larva, Units.Drone };
        yield return new object[] { Units.Hatchery, Units.Queen };
    }

    [Theory]
    [MemberData(nameof(ProducerTypes))]
    public void GivenProducerIsDeadBeforeExpectedCompletionFrame_WhenUpdateStatus_ThenFulfillmentIsPrevented(uint producerUnitType, uint unitTypeToTrain) {
        // Arrange
        var producer = TestUtils.CreateUnit(producerUnitType, _knowledgeBase, actionBuilder: _actionBuilder);
        var producerOrder = producer.TrainUnit(unitTypeToTrain);
        var trainUnitFulfillment = _buildRequestFulfillmentFactory.CreateTrainUnitFulfillment(producer, producerOrder, unitTypeToTrain);

        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns((uint)trainUnitFulfillment.ExpectedCompletionFrame - 1);

        // Act
        trainUnitFulfillment.UpdateStatus();

        // Assert
        Assert.Equal(BuildRequestFulfillmentStatus.Prevented, trainUnitFulfillment.Status);
    }

    [Theory]
    [MemberData(nameof(ProducerTypes))]
    public void GivenProducerIsDeadAtExpectedCompletionFrame_WhenUpdateStatus_ThenFulfillmentIsCompleted(uint producerUnitType, uint unitTypeToTrain) {
        // Arrange
        var producer = TestUtils.CreateUnit(producerUnitType, _knowledgeBase, actionBuilder: _actionBuilder);
        var producerOrder = producer.TrainUnit(unitTypeToTrain);
        var trainUnitFulfillment = _buildRequestFulfillmentFactory.CreateTrainUnitFulfillment(producer, producerOrder, unitTypeToTrain);

        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns((uint)trainUnitFulfillment.ExpectedCompletionFrame);

        // Act
        trainUnitFulfillment.UpdateStatus();

        // Assert
        Assert.Equal(BuildRequestFulfillmentStatus.Completed, trainUnitFulfillment.Status);
    }

    [Fact]
    public void GivenProducerIsBuildingAndAliveAtExpectedCompletionFrame_WhenUpdateStatus_ThenFulfillmentIsCompleted() {
        // Arrange
        var producer = TestUtils.CreateUnit(Units.Hatchery, _knowledgeBase, actionBuilder: _actionBuilder);
        const uint unitTypeToTrain = Units.Queen;
        var producerOrder = producer.TrainUnit(unitTypeToTrain);
        var trainUnitFulfillment = _buildRequestFulfillmentFactory.CreateTrainUnitFulfillment(producer, producerOrder, unitTypeToTrain);

        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns((uint)trainUnitFulfillment.ExpectedCompletionFrame);
        producer.LastSeen = _frameClockMock.Object.CurrentFrame; // Alive

        // Act
        trainUnitFulfillment.UpdateStatus();

        // Assert
        Assert.Equal(BuildRequestFulfillmentStatus.Completed, trainUnitFulfillment.Status);
    }

    private static IEnumerable<object[]> NonMatchingOrders() {
        yield return new object[] { new List<uint>() };
        yield return new object[] { new List<uint> { Units.Marine } };
        yield return new object[] { new List<uint> { Units.Marine, Units.Zealot } };
    }

    [Theory]
    [MemberData(nameof(NonMatchingOrders))]
    public void GivenProducerIsBuildingAndAliveAndHasNoMatchingOrdersAtExpectedCompletionFrame_WhenUpdateStatus_ThenFulfillmentIsCompleted(List<uint> nonMatchingUnitTypes) {
        // Arrange
        var producer = TestUtils.CreateUnit(Units.Hatchery, _knowledgeBase, actionBuilder: _actionBuilder);
        const uint unitTypeToTrain = Units.Queen;
        var producerOrder = producer.TrainUnit(unitTypeToTrain);
        var trainUnitFulfillment = _buildRequestFulfillmentFactory.CreateTrainUnitFulfillment(producer, producerOrder, unitTypeToTrain);

        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns((uint)trainUnitFulfillment.ExpectedCompletionFrame);
        producer.LastSeen = _frameClockMock.Object.CurrentFrame; // Alive

        producer.Orders.Clear();
        foreach (var nonMatchingUnitType in nonMatchingUnitTypes) {
            producer.TrainUnit(nonMatchingUnitType);
        }

        // Act
        trainUnitFulfillment.UpdateStatus();

        // Assert
        Assert.Equal(BuildRequestFulfillmentStatus.Completed, trainUnitFulfillment.Status);
    }

    [Fact]
    public void GivenProducerIsUnitAndAliveAtExpectedCompletionFrame_WhenUpdateStatus_ThenFulfillmentStatusIsExecuting() {
        // Arrange
        var producer = TestUtils.CreateUnit(Units.Larva, _knowledgeBase, actionBuilder: _actionBuilder);
        const uint unitTypeToTrain = Units.Drone;
        var producerOrder = producer.TrainUnit(unitTypeToTrain);
        var trainUnitFulfillment = _buildRequestFulfillmentFactory.CreateTrainUnitFulfillment(producer, producerOrder, unitTypeToTrain);

        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns((uint)trainUnitFulfillment.ExpectedCompletionFrame);
        producer.LastSeen = _frameClockMock.Object.CurrentFrame; // Alive

        // Act
        trainUnitFulfillment.UpdateStatus();

        // Assert
        Assert.Equal(BuildRequestFulfillmentStatus.Executing, trainUnitFulfillment.Status);
    }

    [Theory]
    [MemberData(nameof(NonMatchingOrders))]
    public void GivenProducerIsUnitAndAliveAndHasNoMatchingOrdersAtExpectedCompletionFrame_WhenUpdateStatus_ThenFulfillmentIsAborted(List<uint> nonMatchingUnitTypes) {
        // Arrange
        var producer = TestUtils.CreateUnit(Units.Larva, _knowledgeBase, actionBuilder: _actionBuilder);
        const uint unitTypeToTrain = Units.Drone;
        var producerOrder = producer.TrainUnit(unitTypeToTrain);
        var trainUnitFulfillment = _buildRequestFulfillmentFactory.CreateTrainUnitFulfillment(producer, producerOrder, unitTypeToTrain);

        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns((uint)trainUnitFulfillment.ExpectedCompletionFrame);
        producer.LastSeen = _frameClockMock.Object.CurrentFrame; // Alive

        producer.Orders.Clear();
        foreach (var nonMatchingUnitType in nonMatchingUnitTypes) {
            producer.TrainUnit(nonMatchingUnitType);
        }

        // Act
        trainUnitFulfillment.UpdateStatus();

        // Assert
        Assert.Equal(BuildRequestFulfillmentStatus.Aborted, trainUnitFulfillment.Status);
    }

    private static IEnumerable<object[]> AnyProducerNonMatchingOrders() {
        yield return new object[] { Units.Larva, Units.Drone, new List<uint>() };
        yield return new object[] { Units.Larva, Units.Drone, new List<uint> { Units.Marine } };
        yield return new object[] { Units.Larva, Units.Drone, new List<uint> { Units.Marine, Units.Zealot } };

        yield return new object[] { Units.Hatchery, Units.Queen, new List<uint>() };
        yield return new object[] { Units.Hatchery, Units.Queen, new List<uint> { Units.Marine } };
        yield return new object[] { Units.Hatchery, Units.Queen, new List<uint> { Units.Marine, Units.Zealot } };
    }

    [Theory]
    [MemberData(nameof(AnyProducerNonMatchingOrders))]
    public void GivenProducerIsAliveAndHasNoMatchingOrdersBeforeExpectedCompletionFrame_WhenUpdateStatus_ThenFulfillmentIsAborted(uint producerUnitType, uint unitTypeToTrain, List<uint> nonMatchingUnitTypes) {
        // Arrange
        var producer = TestUtils.CreateUnit(producerUnitType, _knowledgeBase, actionBuilder: _actionBuilder);
        var producerOrder = producer.TrainUnit(unitTypeToTrain);
        var trainUnitFulfillment = _buildRequestFulfillmentFactory.CreateTrainUnitFulfillment(producer, producerOrder, unitTypeToTrain);

        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns((uint)trainUnitFulfillment.ExpectedCompletionFrame - 1);
        producer.LastSeen = _frameClockMock.Object.CurrentFrame; // Alive

        producer.Orders.Clear();
        foreach (var nonMatchingUnitType in nonMatchingUnitTypes) {
            producer.TrainUnit(nonMatchingUnitType);
        }

        // Act
        trainUnitFulfillment.UpdateStatus();

        // Assert
        Assert.Equal(BuildRequestFulfillmentStatus.Aborted, trainUnitFulfillment.Status);
    }

    [Theory]
    [MemberData(nameof(ProducerTypes))]
    public void GivenFulfillmentIsTerminated_WhenUpdateStatus_ThenStatusDoesNotChange(uint producerUnitType, uint unitTypeToTrain) {
        // Arrange
        var producer = TestUtils.CreateUnit(producerUnitType, _knowledgeBase, actionBuilder: _actionBuilder);
        var producerOrder = producer.TrainUnit(unitTypeToTrain);
        var trainUnitFulfillment = _buildRequestFulfillmentFactory.CreateTrainUnitFulfillment(producer, producerOrder, unitTypeToTrain);

        var completionFrame = (uint)trainUnitFulfillment.ExpectedCompletionFrame;
        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns(completionFrame - 1);
        producer.LastSeen = _frameClockMock.Object.CurrentFrame - 1; // Died last frame

        trainUnitFulfillment.UpdateStatus(); // Prevented

        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns(completionFrame);
        producer.LastSeen = _frameClockMock.Object.CurrentFrame - 1; // Died last frame

        // Act
        trainUnitFulfillment.UpdateStatus();

        // Assert
        Assert.Equal(BuildRequestFulfillmentStatus.Prevented, trainUnitFulfillment.Status);
    }
}
