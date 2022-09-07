using Bot.Builds;
using Bot.GameData;
using Bot.Managers;

namespace Bot.Tests.Managers;

public class ManagerTests: BaseTestClass {
    [Fact]
    public void GivenNothing_WhenAssigningUnit_SetsManagerOnUnit() {
        // Arrange
        var manager = new DummyManager();
        var unit = TestUtils.CreateUnit(Units.Zergling);

        // Act
        manager.Assign(unit);

        // Assert
        Assert.Equal(manager, unit.Manager);
    }

    [Fact]
    public void GivenNothing_WhenAssigningUnit_AddsDeathWatcher() {
        // Arrange
        var manager = new DummyManager();
        var unit = TestUtils.CreateUnit(Units.Zergling);

        // Act
        manager.Assign(unit);

        // Assert
        Assert.Contains(manager, unit.DeathWatchers);
    }

    [Fact]
    public void GivenNothing_WhenAssigningUnit_AddsToManagedUnits() {
        // Arrange
        var manager = new DummyManager();
        var unit = TestUtils.CreateUnit(Units.Zergling);

        // Act
        manager.Assign(unit);

        // Assert
        Assert.Contains(unit, manager.ManagedUnits);
    }

    // TODO GD This doesn't really test that nothing happens
    // TODO GD We would need an IAssigner to count the assignments
    [Fact]
    public void GivenManagedUnit_WhenAssigningSameUnit_IgnoresIt() {
        // Arrange
        var manager = new DummyManager();
        var unit = TestUtils.CreateUnit(Units.Zergling);
        manager.Assign(unit);

        // Act
        manager.Assign(unit);

        // Assert
        var managedUnits = manager.ManagedUnits.Where(managed => managed == unit).ToList();
        Assert.Single(managedUnits);

        var deathWatchers = unit.DeathWatchers.Where(deathWatcher => deathWatcher == manager).ToList();
        Assert.Single(deathWatchers);
    }

    [Fact]
    public void GivenManagedUnit_WhenReleasingUnit_UnsetsManagerFromUnit() {
        // Arrange
        var manager = new DummyManager();
        var unit = TestUtils.CreateUnit(Units.Zergling);

        manager.Assign(unit);

        // Act
        manager.Release(unit);

        // Assert
        Assert.Null(unit.Manager);
    }

    [Fact]
    public void GivenManagedUnit_WhenReleasingUnit_RemovesDeathWatcher() {
        // Arrange
        var manager = new DummyManager();
        var unit = TestUtils.CreateUnit(Units.Zergling);

        manager.Assign(unit);

        // Act
        manager.Release(unit);

        // Assert
        Assert.Empty(unit.DeathWatchers);
    }

    [Fact]
    public void GivenManagedUnit_WhenReleasingUnit_RemovesFromManagedUnits() {
        // Arrange
        var manager = new DummyManager();
        var unit = TestUtils.CreateUnit(Units.Zergling);

        manager.Assign(unit);

        // Act
        manager.Release(unit);

        // Assert
        Assert.Empty(manager.ManagedUnits);
    }

    [Fact]
    public void GivenNothing_WhenReleasingUnit_DoesNothing() {
        // Arrange
        var manager = new DummyManager();
        var unit = TestUtils.CreateUnit(Units.Zergling);

        // Act
        var exception = Record.Exception(() => manager.Release(unit));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void GivenManagedUnit_WhenReleasingUnmanagedUnit_DoesNothing() {
        // Arrange
        var manager = new DummyManager();
        var unit = TestUtils.CreateUnit(Units.Zergling);
        var otherUnit = TestUtils.CreateUnit(Units.Zergling);

        manager.Assign(unit);

        // Act
        var exception = Record.Exception(() => manager.Release(otherUnit));

        // Assert
        Assert.Null(exception);
        Assert.Equal(manager, unit.Manager);
        Assert.Contains(manager, unit.DeathWatchers);
        Assert.Contains(unit, manager.ManagedUnits);
    }

    // TODO GD Implement IAssigner, IDispatcher, IReleaser
    private class DummyManager: Manager {
        public override IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();

        protected override IAssigner CreateAssigner() {
            return null!;
        }

        protected override IDispatcher CreateDispatcher() {
            return null!;
        }

        protected override IReleaser CreateReleaser() {
            return null!;
        }

        protected override void AssignUnits() {}

        protected override void DispatchUnits() {}

        protected override void Manage() {}
    }
}
