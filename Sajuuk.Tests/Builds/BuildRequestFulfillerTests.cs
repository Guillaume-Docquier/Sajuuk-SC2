using Moq;
using Sajuuk.Builds;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis;
using Sajuuk.Tests.Fixtures;

namespace Sajuuk.Tests.Builds;

public class BuildRequestFulfillerTests : IClassFixture<NoLoggerFixture> {
    private readonly TechTree _techTree = new TechTree(Mock.Of<IPrerequisiteFactory>()); // TODO GD TechTree should be mockable
    private readonly KnowledgeBase _knowledgeBase = new TestKnowledgeBase();
    private readonly Mock<IUnitsTracker> _unitsTrackerMock = new Mock<IUnitsTracker>();
    private readonly Mock<IBuildingTracker> _buildingTrackerMock = new Mock<IBuildingTracker>();
    private readonly Mock<IPathfinder> _pathfinderMock = new Mock<IPathfinder>();
    private readonly Mock<ITerrainTracker> _terrainTrackerMock = new Mock<ITerrainTracker>();
    private readonly Mock<IController> _controllerMock = new Mock<IController>();
    private readonly Mock<IRegionsTracker> _regionsTrackerMock = new Mock<IRegionsTracker>();

    private readonly BuildRequestFulfiller _buildRequestFulfiller;

    public BuildRequestFulfillerTests() {
        _buildRequestFulfiller = new BuildRequestFulfiller(
            _techTree,
            _knowledgeBase,
            _unitsTrackerMock.Object,
            _buildingTrackerMock.Object,
            _pathfinderMock.Object,
            _terrainTrackerMock.Object,
            _controllerMock.Object,
            _regionsTrackerMock.Object
        );

        _unitsTrackerMock
            .Setup(unitsTracker => unitsTracker.GetUnits(It.IsAny<IEnumerable<Unit>>(), It.IsAny<uint>()))
            .Returns<IEnumerable<Unit>, uint>((unitPool, unitTypeToGet) => unitPool.Where(unit => unit.UnitType == unitTypeToGet));

        _unitsTrackerMock
            .Setup(unitsTracker => unitsTracker.GetUnits(It.IsAny<IEnumerable<Unit>>(), It.IsAny<HashSet<uint>>(), It.IsAny<bool>()))
            .Returns<IEnumerable<Unit>, HashSet<uint>, bool>((unitPool, unitTypesToGet, _) => unitPool.Where(unit => unitTypesToGet.Contains(unit.UnitType)));
    }

    [Fact]
    public void GivenNoDronesAndBuildTypeBuild_WhenFulfillBuildRequest_ThenNoProducersAvailable() {
        // Arrange
        _unitsTrackerMock
            .Setup(unitsTracker => unitsTracker.OwnedUnits)
            .Returns(new List<Unit>());

        var buildRequest = new QuantityBuildRequest(
            BuildType.Build,
            Units.Hatchery,
            quantity: 1,
            atSupply: 0,
            queue: false,
            BuildBlockCondition.None,
            BuildRequestPriority.Low
        );

        // Act
        var result = _buildRequestFulfiller.FulfillBuildRequest(buildRequest);

        // Assert
        var actualNoProducersAvailableFlag = result & BuildRequestResult.NoProducersAvailable;
        Assert.Equal(BuildRequestResult.NoProducersAvailable, actualNoProducersAvailableFlag);
    }

    [Fact]
    public void GivenOneDroneAndBuildTypeBuild_WhenFulfillBuildRequest_ThenWillNotUseTheLastDrone() {
        // Arrange
        _unitsTrackerMock
            .Setup(unitsTracker => unitsTracker.OwnedUnits)
            .Returns(new List<Unit> { TestUtils.CreateUnit(Units.Drone, _knowledgeBase) });

        var buildRequest = new QuantityBuildRequest(
            BuildType.Build,
            Units.Hatchery,
            quantity: 1,
            atSupply: 0,
            queue: false,
            BuildBlockCondition.None,
            BuildRequestPriority.Low
        );

        // Act
        var result = _buildRequestFulfiller.FulfillBuildRequest(buildRequest);

        // Assert
        var actualNoProducersAvailableFlag = result & BuildRequestResult.NoProducersAvailable;
        Assert.Equal(BuildRequestResult.NoProducersAvailable, actualNoProducersAvailableFlag);
    }

    [Fact]
    public void GivenMoreThanOneDroneAndBuildTypeBuild_WhenFulfillBuildRequest_ThenOk() {
        // Arrange
        _unitsTrackerMock
            .Setup(unitsTracker => unitsTracker.OwnedUnits)
            .Returns(new List<Unit>
            {
                TestUtils.CreateUnit(Units.Drone, _knowledgeBase),
                TestUtils.CreateUnit(Units.Drone, _knowledgeBase),
            });

        var buildRequest = new QuantityBuildRequest(
            BuildType.Build,
            Units.Hatchery,
            quantity: 1,
            atSupply: 0,
            queue: false,
            BuildBlockCondition.None,
            BuildRequestPriority.Low
        );

        // Act
        var result = _buildRequestFulfiller.FulfillBuildRequest(buildRequest);

        // Assert
        var actualNoProducersAvailable = result & BuildRequestResult.NoProducersAvailable;
        Assert.Equal(BuildRequestResult.Ok, actualNoProducersAvailable);
    }
}
