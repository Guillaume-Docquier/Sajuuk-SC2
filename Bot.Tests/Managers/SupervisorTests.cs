using Bot.GameData;
using Bot.GameSense;
using Bot.Managers;
using Moq;

namespace Bot.Tests.Managers;

public class SupervisorTests : BaseTestClass {
    private readonly Mock<IUnitsTracker> _unitsTrackerMock;

    public SupervisorTests() {
        _unitsTrackerMock = new Mock<IUnitsTracker>();
    }

    [Fact]
    public void GivenNothing_WhenOnFrame_ThenCallsSupervise() {
        // Arrange
        var supervisor = new OnFrameSupervisor();
        var expected = new List<OnFrameSupervisor.Call>
        {
            OnFrameSupervisor.Call.Supervise
        };

        // Act
        supervisor.OnFrame();

        // Assert
        Assert.Equal(expected, supervisor.CallStack);
    }

    [Fact]
    public void GivenNothing_WhenAssigningMultipleUnits_AssignsEachUnit() {
        // Arrange
        var supervisor = new TestUtils.DummySupervisor();
        var units = new List<Unit>
        {
            TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling),
            TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Drone),
            TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling),
        };

        // Act
        supervisor.Assign(units);

        // Assert
        Assert.Equal(units, supervisor.SupervisedUnits);
    }

    [Fact]
    public void GivenNothing_WhenAssigningUnit_SetsSupervisor() {
        // Arrange
        var supervisor = new TestUtils.DummySupervisor();
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        supervisor.Assign(unit);

        // Assert
        Assert.Equal(supervisor, unit.Supervisor);
    }

    [Fact]
    public void GivenNothing_WhenAssigningUnit_AddsToSupervisedUnits() {
        // Arrange
        var supervisor = new TestUtils.DummySupervisor();
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        supervisor.Assign(unit);

        // Assert
        Assert.Single(supervisor.SupervisedUnits);
        Assert.Contains(unit, supervisor.SupervisedUnits);
    }

    [Fact]
    public void GivenNothing_WhenAssigningUnit_CallsAssigner() {
        // Arrange
        var assigner = new DummyAssigner();
        var supervisor = new TestUtils.DummySupervisor(assigner: assigner);
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        supervisor.Assign(unit);

        // Assert
        Assert.Single(assigner.AssignedUnits);
        Assert.Contains(unit, assigner.AssignedUnits);
    }

    [Fact]
    public void GivenNothing_WhenAssigningUnit_DoesNotAddDeathWatcher() {
        // Arrange
        var supervisor = new TestUtils.DummySupervisor();
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        supervisor.Assign(unit);

        // Assert
        Assert.Empty(unit.DeathWatchers);
    }

    [Fact]
    public void GivenSupervisedUnit_WhenAssigningOtherUnit_SetsSupervisor() {
        // Arrange
        var supervisor = new TestUtils.DummySupervisor();
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        supervisor.Assign(unit);

        var otherUnit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        supervisor.Assign(otherUnit);

        // Assert
        Assert.Equal(supervisor, otherUnit.Supervisor);
    }

    [Fact]
    public void GivenSupervisedUnit_WhenAssigningOtherUnit_AddsToSupervisedUnits() {
        // Arrange
        var supervisor = new TestUtils.DummySupervisor();
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        supervisor.Assign(unit);

        var otherUnit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        supervisor.Assign(otherUnit);

        // Assert
        Assert.Contains(otherUnit, supervisor.SupervisedUnits);
    }

    [Fact]
    public void GivenSupervisedUnit_WhenAssigningOtherUnit_CallsAssigner() {
        // Arrange
        var assigner = new DummyAssigner();
        var supervisor = new TestUtils.DummySupervisor(assigner: assigner);
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        supervisor.Assign(unit);

        var otherUnit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        supervisor.Assign(otherUnit);

        // Assert
        Assert.Contains(otherUnit, assigner.AssignedUnits);
    }

    [Fact]
    public void GivenSupervisedUnit_WhenAssigningOtherUnit_DoesNotAddDeathWatcher() {
        // Arrange
        var supervisor = new TestUtils.DummySupervisor();
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        supervisor.Assign(unit);

        var otherUnit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        supervisor.Assign(otherUnit);

        // Assert
        Assert.Empty(otherUnit.DeathWatchers);
    }

    [Fact]
    public void GivenSupervisedUnit_WhenAssigningSameUnit_DoesNothing() {
        // Arrange
        var assigner = new DummyAssigner();
        var supervisor = new TestUtils.DummySupervisor(assigner: assigner);
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        supervisor.Assign(unit);

        // Act
        supervisor.Assign(unit);

        // Assert
        var assignedUnits = assigner.AssignedUnits.Where(assigned => assigned == unit).ToList();
        Assert.Single(assignedUnits);

        var supervisedUnits = supervisor.SupervisedUnits.Where(supervised => supervised == unit).ToList();
        Assert.Single(supervisedUnits);

        Assert.Empty(unit.DeathWatchers);

        Assert.Equal(supervisor, unit.Supervisor);
    }

    [Fact]
    public void GivenNothing_WhenReleasingUnit_DoesNothing() {
        // Arrange
        var releaser = new DummyReleaser();
        var supervisor = new TestUtils.DummySupervisor(releaser: releaser);
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        supervisor.Release(unit);

        // Assert
        Assert.Empty(releaser.ReleasedUnits);
    }

    [Fact]
    public void GivenSupervisedUnit_WhenReleasingOtherUnit_DoesNothing() {
        // Arrange
        var releaser = new DummyReleaser();
        var supervisor = new TestUtils.DummySupervisor(releaser: releaser);
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        supervisor.Assign(unit);

        var otherUnit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);

        // Act
        supervisor.Release(otherUnit);

        // Assert
        Assert.Empty(releaser.ReleasedUnits);

        Assert.Single(supervisor.SupervisedUnits);
        Assert.Contains(unit, supervisor.SupervisedUnits);

        Assert.Equal(supervisor, unit.Supervisor);
    }

    [Fact]
    public void GivenSupervisedUnit_WhenReleasingUnit_UnsetsSupervisor() {
        // Arrange
        var releaser = new DummyReleaser();
        var supervisor = new TestUtils.DummySupervisor(releaser: releaser);
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        supervisor.Assign(unit);

        // Act
        supervisor.Release(unit);

        // Assert
        Assert.Null(unit.Supervisor);
    }

    [Fact]
    public void GivenSupervisedUnit_WhenReleasingUnit_CallsReleaser() {
        // Arrange
        var releaser = new DummyReleaser();
        var supervisor = new TestUtils.DummySupervisor(releaser: releaser);
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        supervisor.Assign(unit);

        // Act
        supervisor.Release(unit);

        // Assert
        Assert.Single(releaser.ReleasedUnits);
        Assert.Contains(unit, releaser.ReleasedUnits);
    }

    [Fact]
    public void GivenSupervisedUnit_WhenReleasingUnit_RemovesFromSupervisedUnits() {
        // Arrange
        var releaser = new DummyReleaser();
        var supervisor = new TestUtils.DummySupervisor(releaser: releaser);
        var unit = TestUtils.CreateUnit(_unitsTrackerMock.Object, Units.Zergling);
        supervisor.Assign(unit);

        // Act
        supervisor.Release(unit);

        // Assert
        Assert.Empty(supervisor.SupervisedUnits);
    }

    private class OnFrameSupervisor : TestUtils.DummySupervisor {
        public enum Call {
            Supervise,
        }

        public readonly List<Call> CallStack = new List<Call>();

        protected override void Supervise() {
            CallStack.Add(Call.Supervise);
        }
    }

    private class DummyAssigner: IAssigner {
        public List<Unit> AssignedUnits { get; } = new List<Unit>();

        public void Assign(Unit unit) {
            AssignedUnits.Add(unit);
        }
    }

    private class DummyReleaser: IReleaser {
        public List<Unit> ReleasedUnits { get; } = new List<Unit>();

        public void Release(Unit unit) {
            ReleasedUnits.Add(unit);
        }
    }
}
