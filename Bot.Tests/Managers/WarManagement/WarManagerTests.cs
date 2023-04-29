using Bot.Debugging;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
using Bot.MapKnowledge;
using Bot.Tagging;
using Bot.Tests.Mocks;
using Moq;

namespace Bot.Tests.Managers.WarManagement;

public class WarManagerTests : BaseTestClass {
    private readonly Mock<ITaggingService> _taggingServiceMock;
    private readonly Mock<IEnemyRaceTracker> _enemyRaceTrackerMock;
    private readonly Mock<IVisibilityTracker> _visibilityTrackerMock;
    private readonly Mock<IDebuggingFlagsTracker> _debuggingFlagsTrackerMock;
    private readonly TestUnitsTracker _unitsTracker;
    private readonly Mock<IMapAnalyzer> _mapAnalyzerMock;
    private readonly Mock<IExpandAnalyzer> _expandAnalyzerMock;
    private readonly Mock<IRegionAnalyzer> _regionAnalyzerMock;
    private readonly Mock<IRegionsEvaluationsTracker> _regionTrackerMock;

    public WarManagerTests() {
        _taggingServiceMock = new Mock<ITaggingService>();
        _enemyRaceTrackerMock = new Mock<IEnemyRaceTracker>();
        _visibilityTrackerMock = new Mock<IVisibilityTracker>();
        _debuggingFlagsTrackerMock = new Mock<IDebuggingFlagsTracker>();
        _unitsTracker = new TestUnitsTracker();
        _mapAnalyzerMock = new Mock<IMapAnalyzer>();
        _expandAnalyzerMock = new Mock<IExpandAnalyzer>();
        _regionAnalyzerMock = new Mock<IRegionAnalyzer>();
        _regionTrackerMock = new Mock<IRegionsEvaluationsTracker>();
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenUnmanagedUnits_WhenOnFrame_ManagesMilitaryUnits() {
        // Arrange
        var manager = new Bot.Managers.WarManagement.WarManager(
            _taggingServiceMock.Object,
            _enemyRaceTrackerMock.Object,
            _visibilityTrackerMock.Object,
            _debuggingFlagsTrackerMock.Object,
            _unitsTracker,
            _mapAnalyzerMock.Object,
            _expandAnalyzerMock.Object,
            _regionAnalyzerMock.Object,
            _regionTrackerMock.Object
        );

        var militaryUnits = Units.ZergMilitary
            .Except(new HashSet<uint> { Units.Queen, Units.QueenBurrowed })
            .Select(militaryUnitType => TestUtils.CreateUnit(_unitsTracker, militaryUnitType))
            .ToList();

        _unitsTracker.SetUnits(militaryUnits);

        // Act
        manager.OnFrame();

        // Assert
        Assert.All(militaryUnits, militaryUnit => Assert.Equal(manager, militaryUnit.Manager));
    }

    [Fact(Skip = "Not yet implemented")]
    public void GivenUnManagedUnit_WhenOnFrame_DoesNotManageNonMilitaryUnits() {
        // Arrange
        var manager = new Bot.Managers.WarManagement.WarManager(
            _taggingServiceMock.Object,
            _enemyRaceTrackerMock.Object,
            _visibilityTrackerMock.Object,
            _debuggingFlagsTrackerMock.Object,
            _unitsTracker,
            _mapAnalyzerMock.Object,
            _expandAnalyzerMock.Object,
            _regionAnalyzerMock.Object,
            _regionTrackerMock.Object
        );
        // TODO UnitsTracker

        // Act
        manager.OnFrame();

        // Assert
    }
}
