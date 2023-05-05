using Bot.GameData;
using Bot.Managers.WarManagement.States;
using Bot.Tests.Mocks;
using Moq;

namespace Bot.Tests.Managers.WarManagement;

public class WarManagerTests : BaseTestClass {
    private readonly TestUnitsTracker _unitsTracker;
    private readonly Mock<IWarManagerStateFactory> _warManagerStateFactoryMock;

    public WarManagerTests() {
        _unitsTracker = new TestUnitsTracker();
        _warManagerStateFactoryMock = new Mock<IWarManagerStateFactory>();
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenUnmanagedUnits_WhenOnFrame_ManagesMilitaryUnits() {
        // Arrange
        var manager = new Bot.Managers.WarManagement.WarManager(_warManagerStateFactoryMock.Object);

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
        var manager = new Bot.Managers.WarManagement.WarManager(_warManagerStateFactoryMock.Object);
        // TODO UnitsTracker

        // Act
        manager.OnFrame();

        // Assert
    }
}
