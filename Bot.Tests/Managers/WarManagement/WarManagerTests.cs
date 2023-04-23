using Bot.Debugging;
using Bot.GameData;
using Bot.GameSense;
using Bot.Tagging;
using Moq;

namespace Bot.Tests.Managers.WarManagement;

public class WarManagerTests : BaseTestClass {
    private readonly Mock<ITaggingService> _taggingServiceMock;
    private readonly Mock<IEnemyRaceTracker> _enemyRaceTrackerMock;
    private readonly Mock<IVisibilityTracker> _visibilityTrackerMock;
    private readonly Mock<IDebuggingFlagsTracker> _debuggingFlagsTrackerMock;

    public WarManagerTests() {
        _taggingServiceMock = new Mock<ITaggingService>();
        _enemyRaceTrackerMock = new Mock<IEnemyRaceTracker>();
        _visibilityTrackerMock = new Mock<IVisibilityTracker>();
        _debuggingFlagsTrackerMock = new Mock<IDebuggingFlagsTracker>();
    }

    [Fact]
    public void GivenUnManagedUnit_WhenOnFrame_ManagesMilitaryUnits() {
        // Arrange
        var manager = new Bot.Managers.WarManagement.WarManager(
            _taggingServiceMock.Object,
            _enemyRaceTrackerMock.Object,
            _visibilityTrackerMock.Object,
            _debuggingFlagsTrackerMock.Object
        );

        var militaryUnits = Units.ZergMilitary
            .Except(new HashSet<uint> { Units.Queen, Units.QueenBurrowed })
            .Select(militaryUnitType => TestUtils.CreateUnit(militaryUnitType))
            .ToList();

        var setupUnits = UnitsTracker.UnitsByTag.Values;

        TestUtils.NewFrame(ResponseGameObservationUtils.CreateResponseObservation(
            units: militaryUnits.Concat(setupUnits),
            frame: 1
        ));

        var newOwnedUnits = UnitsTracker.NewOwnedUnits;

        // Act
        manager.OnFrame();

        // Assert
        Assert.All(newOwnedUnits, militaryUnit => Assert.Equal(manager, militaryUnit.Manager));
    }

    [Fact(Skip = "Not yet implemented")]
    public void GivenUnManagedUnit_WhenOnFrame_DoesNotManageNonMilitaryUnits() {
        // Arrange
        var manager = new Bot.Managers.WarManagement.WarManager(
            _taggingServiceMock.Object,
            _enemyRaceTrackerMock.Object,
            _visibilityTrackerMock.Object,
            _debuggingFlagsTrackerMock.Object
        );
        // TODO UnitsTracker

        // Act
        manager.OnFrame();

        // Assert
    }
}
