using Bot.Debugging.GraphicalDebugging;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers.WarManagement.States;
using Bot.Tests.Mocks;
using Bot.Wrapper;
using Moq;

namespace Bot.Tests.Managers.WarManagement;

public class WarManagerTests : BaseTestClass {
    private readonly Mock<IFrameClock> _frameClockMock;
    private readonly IActionBuilder _actionBuilder;
    private readonly Mock<IActionService> _actionServiceMock;
    private readonly Mock<ITerrainTracker> _terrainTrackerMock;
    private readonly Mock<IRegionsTracker> _regionsTrackerMock;
    private readonly TestUnitsTracker _unitsTracker;
    private readonly Mock<IWarManagerStateFactory> _warManagerStateFactoryMock;
    private readonly Mock<IGraphicalDebugger> _graphicalDebuggerMock;

    public WarManagerTests() {
        _frameClockMock = new Mock<IFrameClock>();
        _actionBuilder = new ActionBuilder(KnowledgeBase);
        _actionServiceMock = new Mock<IActionService>();
        _terrainTrackerMock = new Mock<ITerrainTracker>();
        _regionsTrackerMock = new Mock<IRegionsTracker>();
        _unitsTracker = new TestUnitsTracker();
        _warManagerStateFactoryMock = new Mock<IWarManagerStateFactory>();
        _graphicalDebuggerMock = new Mock<IGraphicalDebugger>();
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenUnmanagedUnits_WhenOnFrame_ManagesMilitaryUnits() {
        // Arrange
        var manager = new Bot.Managers.WarManagement.WarManager(_warManagerStateFactoryMock.Object, _graphicalDebuggerMock.Object);

        var militaryUnits = Units.ZergMilitary
            .Except(new HashSet<uint> { Units.Queen, Units.QueenBurrowed })
            .Select(militaryUnitType => TestUtils.CreateUnit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, militaryUnitType))
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
        var manager = new Bot.Managers.WarManagement.WarManager(_warManagerStateFactoryMock.Object, _graphicalDebuggerMock.Object);
        // TODO UnitsTracker

        // Act
        manager.OnFrame();

        // Assert
    }
}
