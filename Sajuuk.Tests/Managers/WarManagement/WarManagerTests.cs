using System.Numerics;
using Sajuuk.Actions;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.Managers.WarManagement.States;
using Sajuuk.Tests.Mocks;
using Moq;
using SC2APIProtocol;

namespace Sajuuk.Tests.Managers.WarManagement;

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
        var manager = new Sajuuk.Managers.WarManagement.WarManager(_warManagerStateFactoryMock.Object, _graphicalDebuggerMock.Object);

        var militaryUnits = Units.ZergMilitary
            .Except(new HashSet<uint> { Units.Queen, Units.QueenBurrowed })
            .Select(militaryUnitType => CreateUnit(militaryUnitType))
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
        var manager = new Sajuuk.Managers.WarManagement.WarManager(_warManagerStateFactoryMock.Object, _graphicalDebuggerMock.Object);
        // TODO UnitsTracker

        // Act
        manager.OnFrame();

        // Assert
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
            KnowledgeBase, _frameClockMock.Object, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker,
            frame, alliance, position, vespeneContents, buildProgress
        );
    }
}
