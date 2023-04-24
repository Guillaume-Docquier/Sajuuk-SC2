using Bot.GameData;
using Bot.GameSense;
using Bot.Managers;
using Moq;

namespace Bot.Tests.Managers;

public class ManagerTests : BaseTestClass {
    private readonly Mock<IUnitsTracker> _unitsTrackerMock;

    public ManagerTests() {
        _unitsTrackerMock = new Mock<IUnitsTracker>();
    }

    [Fact]
    public void GivenNothing_WhenAssigningMultipleUnits_AssignsEachUnit() {
        // Arrange
        var manager = new TestUtils.DummyManager();
        var units = new List<Unit>
        {
            TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling),
            TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Drone),
            TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling),
        };

        // Act
        manager.Assign(units);

        // Assert
        Assert.Equal(units, manager.ManagedUnits.ToList());
    }

    [Fact]
    public void GivenNothing_WhenAssigningUnit_SetsManagerOnUnit() {
        // Arrange
        var manager = new TestUtils.DummyManager();
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        manager.Assign(unit);

        // Assert
        Assert.Equal(manager, unit.Manager);
    }

    [Fact]
    public void GivenNothing_WhenAssigningUnit_AddsDeathWatcher() {
        // Arrange
        var manager = new TestUtils.DummyManager();
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        manager.Assign(unit);

        // Assert
        Assert.Contains(manager, unit.DeathWatchers);
    }

    [Fact]
    public void GivenNothing_WhenAssigningUnit_AddsToManagedUnits() {
        // Arrange
        var manager = new TestUtils.DummyManager();
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        manager.Assign(unit);

        // Assert
        Assert.Contains(unit, manager.ManagedUnits);
    }

    [Fact]
    public void GivenNothing_WhenAssigningUnit_CallsAssigner() {
        // Arrange
        var assigner = new DummyAssigner();
        var manager = new TestUtils.DummyManager(assigner: assigner);
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        manager.Assign(unit);

        // Assert
        Assert.Contains(unit, assigner.AssignedUnits);
    }

    [Fact]
    public void GivenManagedUnit_WhenAssigningOtherUnit_SetsManagerOnUnit() {
        // Arrange
        var manager = new TestUtils.DummyManager();
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        manager.Assign(unit);

        var otherUnit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        manager.Assign(otherUnit);

        // Assert
        Assert.Equal(manager, otherUnit.Manager);
    }

    [Fact]
    public void GivenManagedUnit_WhenAssigningOtherUnit_AddsDeathWatcher() {
        // Arrange
        var manager = new TestUtils.DummyManager();
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        manager.Assign(unit);

        var otherUnit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        manager.Assign(otherUnit);

        // Assert
        Assert.Contains(manager, otherUnit.DeathWatchers);
    }

    [Fact]
    public void GivenManagedUnit_WhenAssigningOtherUnit_AddsToManagedUnits() {
        // Arrange
        var manager = new TestUtils.DummyManager();
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        manager.Assign(unit);

        var otherUnit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        manager.Assign(otherUnit);

        // Assert
        Assert.Contains(otherUnit, manager.ManagedUnits);
    }

    [Fact]
    public void GivenManagedUnit_WhenAssigningOtherUnit_CallsAssigner() {
        // Arrange
        var assigner = new DummyAssigner();
        var manager = new TestUtils.DummyManager(assigner: assigner);
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        manager.Assign(unit);

        var otherUnit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        manager.Assign(otherUnit);

        // Assert
        Assert.Contains(otherUnit, assigner.AssignedUnits);
    }

    [Fact]
    public void GivenManagedUnit_WhenAssigningSameUnit_DoesNothing() {
        // Arrange
        var assigner = new DummyAssigner();
        var manager = new TestUtils.DummyManager(assigner: assigner);
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        manager.Assign(unit);

        // Act
        manager.Assign(unit);

        // Assert
        var assignedUnits = assigner.AssignedUnits.Where(assigned => assigned == unit).ToList();
        Assert.Single(assignedUnits);

        var managedUnits = manager.ManagedUnits.Where(managed => managed == unit).ToList();
        Assert.Single(managedUnits);

        var deathWatchers = unit.DeathWatchers.Where(deathWatcher => deathWatcher == manager).ToList();
        Assert.Single(deathWatchers);

        Assert.Equal(manager, unit.Manager);
    }

    [Fact]
    public void GivenNothing_WhenReleasingMultipleUnits_ReleasesEachUnit() {
        // Arrange
        var manager = new TestUtils.DummyManager();
        var units = new List<Unit>
        {
            TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling),
            TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Drone),
            TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling),
        };

        manager.Assign(units);

        // Act
        manager.Release(units);

        // Assert
        Assert.Empty(manager.ManagedUnits);
    }

    [Fact]
    public void GivenManagedUnit_WhenReleasingUnit_UnsetsManagerFromUnit() {
        // Arrange
        var manager = new TestUtils.DummyManager();
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        manager.Assign(unit);

        // Act
        manager.Release(unit);

        // Assert
        Assert.Null(unit.Manager);
    }

    [Fact]
    public void GivenManagedUnit_WhenReleasingUnit_RemovesDeathWatcher() {
        // Arrange
        var manager = new TestUtils.DummyManager();
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        manager.Assign(unit);

        // Act
        manager.Release(unit);

        // Assert
        Assert.Empty(unit.DeathWatchers);
    }

    [Fact]
    public void GivenManagedUnit_WhenReleasingUnit_RemovesFromManagedUnits() {
        // Arrange
        var manager = new TestUtils.DummyManager();
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        manager.Assign(unit);

        // Act
        manager.Release(unit);

        // Assert
        Assert.Empty(manager.ManagedUnits);
    }

    [Fact]
    public void GivenManagedUnit_WhenReleasingUnit_ReleasesSupervisor() {
        // Arrange
        var manager = new TestUtils.DummyManager();
        var supervisor = new TestUtils.DummySupervisor();
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        manager.Assign(unit);
        supervisor.Assign(unit);

        // Act
        manager.Release(unit);

        // Assert
        Assert.Empty(manager.ManagedUnits);
        Assert.Empty(supervisor.SupervisedUnits);
    }

    [Fact]
    public void GivenManagedUnit_WhenReleasingUnit_CallsReleaser() {
        // Arrange
        var releaser = new DummyReleaser();
        var manager = new TestUtils.DummyManager(releaser: releaser);
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        manager.Assign(unit);

        // Act
        manager.Release(unit);

        // Assert
        Assert.Contains(unit, releaser.ReleasedUnits);
    }

    [Fact]
    public void GivenNothing_WhenReleasingUnit_DoesNothing() {
        // Arrange
        var manager = new TestUtils.DummyManager();
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        var exception = Record.Exception(() => manager.Release(unit));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void GivenManagedUnit_WhenReleasingUnmanagedUnit_DoesNothing() {
        // Arrange
        var releaser = new DummyReleaser();
        var manager = new TestUtils.DummyManager(releaser: releaser);
        var supervisor = new TestUtils.DummySupervisor();

        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        var otherUnit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        manager.Assign(unit);
        supervisor.Assign(unit);

        // Act
        var exception = Record.Exception(() => manager.Release(otherUnit));

        // Assert
        Assert.Null(exception);
        Assert.Equal(manager, unit.Manager);
        Assert.Equal(supervisor, unit.Supervisor);
        Assert.Contains(unit, manager.ManagedUnits);
        Assert.Contains(unit, supervisor.SupervisedUnits);
        Assert.Contains(manager, unit.DeathWatchers);
    }

    [Fact]
    public void GivenNothing_WhenOnFrame_CallsAssignDispatchManage() {
        // Arrange
        var manager = new OnFrameManager();
        var expected = new List<OnFrameManager.Call>
        {
            OnFrameManager.Call.Assign,
            OnFrameManager.Call.Dispatch,
            OnFrameManager.Call.Manage,
        };

        // Act
        manager.OnFrame();

        // Assert
        Assert.Equal(expected, manager.CallStack);
    }

    [Fact]
    public void GivenManagedUnits_WhenDispatching_DispatchesAllUnits() {
        // Arrange
        var dispatcher = new DummyDispatcher();
        var manager = new TestUtils.DummyManager(dispatcher: dispatcher);
        var units = new List<Unit>
        {
            TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling),
            TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling),
            TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling),
        };

        manager.Assign(units);

        // Act
        manager.Dispatch(units);

        // Assert
        Assert.Equal(units, dispatcher.DispatchedUnits);
    }

    [Fact]
    public void GivenNothing_WhenDispatching_DoesNothing() {
        // Arrange
        var dispatcher = new DummyDispatcher();
        var manager = new TestUtils.DummyManager(dispatcher: dispatcher);
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        manager.Dispatch(unit);

        // Assert
        Assert.Empty(dispatcher.DispatchedUnits);
    }

    [Fact]
    public void GivenManagedUnit_WhenDispatching_CallsDispatcher() {
        // Arrange
        var dispatcher = new DummyDispatcher();
        var manager = new TestUtils.DummyManager(dispatcher: dispatcher);
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        manager.Assign(unit);

        // Act
        manager.Dispatch(unit);

        // Assert
        Assert.Single(dispatcher.DispatchedUnits);
        Assert.Contains(unit, dispatcher.DispatchedUnits);
    }

    [Fact]
    public void GivenManagedUnit_WhenDispatchingUnmanagedUnit_DoesNothing() {
        // Arrange
        var dispatcher = new DummyDispatcher();
        var manager = new TestUtils.DummyManager(dispatcher: dispatcher);
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        manager.Assign(unit);

        var unmanagedUnit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        manager.Dispatch(unmanagedUnit);

        // Assert
        Assert.Empty(dispatcher.DispatchedUnits);
    }

    [Fact]
    public void GivenManagedUnit_WhenUnitDies_ReleasesUnit() {
        // Arrange
        var releaser = new DummyReleaser();
        var manager = new TestUtils.DummyManager(releaser: releaser);
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        manager.Assign(unit);

        // Act
        unit.Died();

        // Assert
        Assert.Single(releaser.ReleasedUnits);
        Assert.Contains(unit, releaser.ReleasedUnits);

        Assert.Empty(manager.ManagedUnits);
    }

    [Fact]
    public void GivenNothing_WhenUnmanagedUnitDies_DoesNothing() {
        // Arrange
        var releaser = new DummyReleaser();
        var manager = new TestUtils.DummyManager(releaser: releaser);
        var unmanagedUnit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        unmanagedUnit.Died();

        // Assert
        Assert.Empty(releaser.ReleasedUnits);

        Assert.Empty(manager.ManagedUnits);
    }

    [Fact]
    public void GivenManagedUnit_WhenUnmanagedUnitDies_DoesNothing() {
        // Arrange
        var releaser = new DummyReleaser();
        var manager = new TestUtils.DummyManager(releaser: releaser);
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        manager.Assign(unit);

        var unmanagedUnit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        unmanagedUnit.Died();

        // Assert
        Assert.Empty(releaser.ReleasedUnits);

        Assert.Single(manager.ManagedUnits);
        Assert.Contains(unit, manager.ManagedUnits);
    }

    [Fact]
    public void GivenManagedUnit_WhenHandlingDeathOfUnmanagedUnit_DoesNothing() {
        // Arrange
        var releaser = new DummyReleaser();
        var manager = new TestUtils.DummyManager(releaser: releaser);
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        manager.Assign(unit);

        var unmanagedUnit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        unmanagedUnit.AddDeathWatcher(manager);

        // Act
        unmanagedUnit.Died();

        // Assert
        Assert.Empty(releaser.ReleasedUnits);

        Assert.Single(manager.ManagedUnits);
        Assert.Contains(unit, manager.ManagedUnits);
    }

    private class DummyAssigner: IAssigner {
        public List<Unit> AssignedUnits { get; } = new List<Unit>();

        public void Assign(Unit unit) {
            AssignedUnits.Add(unit);
        }
    }

    private class DummyDispatcher: IDispatcher {
        public List<Unit> DispatchedUnits { get; } = new List<Unit>();

        public void Dispatch(Unit unit) {
            DispatchedUnits.Add(unit);
        }
    }

    private class DummyReleaser: IReleaser {
        public List<Unit> ReleasedUnits { get; } = new List<Unit>();

        public void Release(Unit unit) {
            ReleasedUnits.Add(unit);
        }
    }

    private class OnFrameManager : TestUtils.DummyManager {
        public enum Call {
            Assign,
            Dispatch,
            Manage,
        }

        public readonly List<Call> CallStack = new List<Call>();

        protected override void RecruitmentPhase() {
            CallStack.Add(Call.Assign);
        }

        protected override void DispatchPhase() {
            CallStack.Add(Call.Dispatch);
        }

        protected override void ManagementPhase() {
            CallStack.Add(Call.Manage);
        }
    }
}
