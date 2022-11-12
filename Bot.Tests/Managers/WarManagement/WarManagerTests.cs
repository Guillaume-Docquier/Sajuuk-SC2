using Bot.GameData;
using Bot.GameSense;

namespace Bot.Tests.Managers.WarManagement;

public class WarManagerTests : BaseTestClass {
    [Fact]
    public void GivenUnManagedUnit_WhenOnFrame_ManagesMilitaryUnits() {
        // Arrange
        var manager = new Bot.Managers.WarManagement.WarManager();

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
        var manager = new Bot.Managers.WarManagement.WarManager();
        // TODO UnitsTracker

        // Act
        manager.OnFrame();

        // Assert
    }
}
