using System.Numerics;
using Sajuuk.Actions;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Moq;
using SC2APIProtocol;

namespace Sajuuk.Tests;

public class UnitTests : BaseTestClass {
    private readonly Mock<IFrameClock> _frameClockMock;
    private readonly IActionBuilder _actionBuilder;
    private readonly Mock<IActionService> _actionServiceMock;
    private readonly Mock<ITerrainTracker> _terrainTrackerMock;
    private readonly Mock<IRegionsTracker> _regionsTrackerMock;
    private readonly Mock<IUnitsTracker> _unitsTrackerMock;

    public UnitTests() {
        _frameClockMock = new Mock<IFrameClock>();
        _actionBuilder = new ActionBuilder(KnowledgeBase);
        _actionServiceMock = new Mock<IActionService>();
        _terrainTrackerMock = new Mock<ITerrainTracker>();
        _regionsTrackerMock = new Mock<IRegionsTracker>();
        _unitsTrackerMock = new Mock<IUnitsTracker>();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void Given1FrameDeathDelay_WhenOutOfVision_DiesAfter1Frame(ulong outOfVisionTime, bool expected) {
        // Arrange
        var unit = CreateUnit(Units.Zergling, frame: 0);

        // Act
        var isDead = unit.IsDead(outOfVisionTime);

        // Assert
        Assert.Equal(expected, isDead);
    }

    [Fact]
    public void GivenDeathWatcherThatRemovesItself_WhenDies_DoesNotThrow() {
        // Arrange
        var unit = CreateUnit(Units.Zergling);

        var deathWatcher = new DeathWatcherThatRemovesItself();
        unit.AddDeathWatcher(deathWatcher);

        // Act
        Assert.False(deathWatcher.ReportedDeath);
        var exception = Record.Exception(() => unit.Died());

        //Assert
        Assert.Null(exception);
        Assert.True(deathWatcher.ReportedDeath);
    }

    [Fact]
    public void GivenManager_WhenPlaceBuilding_IsReleased() {
        // Arrange
        var unit = CreateUnit(Units.Drone);

        var manager = new TestUtils.DummyManager();
        manager.Assign(unit);

        // Act
        Assert.Contains(unit, manager.ManagedUnits);
        unit.PlaceBuilding(Units.Hatchery, new Vector2());

        //Assert
        Assert.Null(unit.Manager);
        Assert.DoesNotContain(unit, manager.ManagedUnits);
    }

    [Fact]
    public void GivenManager_WhenPlaceExtractor_IsReleased() {
        // Arrange
        var unit = CreateUnit(Units.Drone);
        var geyser = CreateUnit(Units.VespeneGeyser);

        var manager = new TestUtils.DummyManager();
        manager.Assign(unit);

        // Act
        Assert.Contains(unit, manager.ManagedUnits);
        unit.PlaceExtractor(Units.Extractor, geyser);

        //Assert
        Assert.Null(unit.Manager);
        Assert.DoesNotContain(unit, manager.ManagedUnits);
    }

    [Fact]
    public void GivenNoManager_WhenPlaceBuilding_DoesNotThrow() {
        // Arrange
        var unit = CreateUnit(Units.Drone);

        // Act
        Assert.Null(unit.Manager);
        var exception = Record.Exception(() => unit.PlaceBuilding(Units.Hatchery, new Vector2()));

        //Assert
        Assert.Null(exception);
    }

    [Fact]
    public void GivenNoManager_WhenPlaceExtractor_DoesNotThrow() {
        // Arrange
        var unit = CreateUnit(Units.Drone);
        var geyser = CreateUnit(Units.VespeneGeyser);

        // Act
        Assert.Null(unit.Manager);
        var exception = Record.Exception(() => unit.PlaceExtractor(Units.Extractor, geyser));

        //Assert
        Assert.Null(exception);
    }

    private class DeathWatcherThatRemovesItself: IWatchUnitsDie {
        public bool ReportedDeath = false;

        public void ReportUnitDeath(Unit deadUnit) {
            deadUnit.RemoveDeathWatcher(this);
            ReportedDeath = true;
        }
    }

    private Unit CreateUnit(
        uint unitType,
        uint frame = 0,
        Alliance alliance = Alliance.Self,
        Vector3 position = default,
        int vespeneContents = 0,
        float buildProgress = 1f
    ) {
        return TestUtils.CreateUnit(
            unitType,
            KnowledgeBase, _frameClockMock.Object, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTrackerMock.Object,
            frame, alliance, position, vespeneContents, buildProgress
        );
    }
}
