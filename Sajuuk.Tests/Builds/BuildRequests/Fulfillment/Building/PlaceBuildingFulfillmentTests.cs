using System.Numerics;
using Moq;
using Sajuuk.Actions;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.Builds.BuildRequests.Fulfillment;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis;
using Sajuuk.Tests.Fixtures;

namespace Sajuuk.Tests.Builds.BuildRequests.Fulfillment.Building;

public class PlaceBuildingFulfillmentTests : IClassFixture<NoLoggerFixture> {
    private readonly Mock<IUnitsTracker> _unitsTrackerMock = new Mock<IUnitsTracker>();
    private readonly Mock<IFrameClock> _frameClockMock = new Mock<IFrameClock>();
    private readonly KnowledgeBase _knowledgeBase = new TestKnowledgeBase();
    private readonly Mock<IPathfinder> _pathfinderMock = new Mock<IPathfinder>();
    private readonly Mock<ITerrainTracker> _terrainTrackerMock = new Mock<ITerrainTracker>();
    private readonly Mock<IController> _controllerMock = new Mock<IController>();
    private readonly IActionBuilder _actionBuilder;

    private readonly BuildRequestFulfillmentFactory _buildRequestFulfillmentFactory;

    public PlaceBuildingFulfillmentTests() {
        _actionBuilder = new ActionBuilder(_knowledgeBase);

        _frameClockMock.Setup(frameClock => frameClock.CurrentFrame).Returns(0);
        _pathfinderMock
            .Setup(pathfinder => pathfinder.FindPath(It.IsAny<Vector2>(), It.IsAny<Vector2>()))
            .Returns<Vector2, Vector2>((origin, destination) => new List<Vector2> { origin, destination });

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
        yield return new object[] { new DummyBuildRequest(BuildType.Build, Units.Hatchery)       , true };
        yield return new object[] { new DummyBuildRequest(BuildType.Train, Units.Hatchery)       , false };
        yield return new object[] { new DummyBuildRequest(BuildType.Research, Units.Hatchery)    , false };
        yield return new object[] { new DummyBuildRequest(BuildType.Expand, Units.Hatchery)      , false };
        yield return new object[] { new DummyBuildRequest(BuildType.Build, Units.SpawningPool)   , false };
        yield return new object[] { new DummyBuildRequest(BuildType.Train, Units.SpawningPool)   , false };
        yield return new object[] { new DummyBuildRequest(BuildType.Research, Units.SpawningPool), false };
        yield return new object[] { new DummyBuildRequest(BuildType.Expand, Units.SpawningPool)  , false };
    }

    [Theory]
    [MemberData(nameof(BuildRequestsToSatisfy))]
    public void GivenBuildRequest_WhenCanSatisfy_ThenReturnsTrueIfBuildTypeIsBuildAndUnitTypeMatches(IBuildRequest buildRequest, bool expectedCanSatisfy) {
        // Arrange
        var producer = TestUtils.CreateUnit(Units.Drone, _knowledgeBase, actionBuilder: _actionBuilder);

        var buildingLocation = new Vector2(0, 0);
        const uint buildingTypeToPlace = Units.Hatchery;
        var producerOrder = producer.PlaceBuilding(buildingTypeToPlace, buildingLocation);

        var placeBuildingFulfillment = _buildRequestFulfillmentFactory.CreatePlaceBuildingFulfillment(producer, producerOrder, buildingTypeToPlace);

        // Act
        var canSatisfy = placeBuildingFulfillment.CanSatisfy(buildRequest);

        // Assert
        Assert.Equal(expectedCanSatisfy, canSatisfy);
    }
}
