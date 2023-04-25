using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameData;
using Bot.MapKnowledge;
using Bot.Tests.Mocks;
using Moq;

namespace Bot.Tests.Managers.EconomyManagement.TownHallSupervision;

public class TownHallSupervisorTests : BaseTestClass {
    private readonly TestUnitsTracker _unitsTracker;
    private readonly TestBuildingTracker _buildingTracker;
    private readonly Mock<IExpandAnalyzer> _expandAnalyzerMock;

    public TownHallSupervisorTests() {
        _unitsTracker = new TestUnitsTracker();
        _buildingTracker = new TestBuildingTracker();
        _expandAnalyzerMock = new Mock<IExpandAnalyzer>();
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenFarMineral_WhenNewTownHallSupervisor_ThenDoesNotAssignMineral() {
        // Arrange
        var townHall = TestUtils.CreateUnit(_unitsTracker, Units.Hatchery);
        var farMineral = TestUtils.CreateUnit(_unitsTracker, Units.MineralField450, position: new Vector3(10, 10, 10));
        _unitsTracker.SetUnits(new List<Unit> { townHall, farMineral });

        // Act
        var townHallSupervisor = new Bot.Managers.EconomyManagement.TownHallSupervision.TownHallSupervisor(_unitsTracker, _buildingTracker, _expandAnalyzerMock.Object, townHall, Colors.Cyan);

        // Assert
        Assert.Null(farMineral.Supervisor);
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenCloseMineral_WhenNewTownHallSupervisor_ThenAssignMineral() {
        // Arrange
        var townHall = TestUtils.CreateUnit(_unitsTracker, Units.Hatchery);
        var closeMineral = TestUtils.CreateUnit(_unitsTracker, Units.MineralField450);
        _unitsTracker.SetUnits(new List<Unit> { townHall, closeMineral });

        // Act
        var townHallSupervisor = new Bot.Managers.EconomyManagement.TownHallSupervision.TownHallSupervisor(_unitsTracker, _buildingTracker, _expandAnalyzerMock.Object, townHall, Colors.Cyan);

        // Assert
        Assert.Equal(townHallSupervisor, closeMineral.Supervisor);
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenTownHallAndMineral_WhenNewTownHallSupervisor_ThenCapacityIsSet() {
        // Arrange
        var townHall = TestUtils.CreateUnit(_unitsTracker, Units.Hatchery);
        var closeMineral = TestUtils.CreateUnit(_unitsTracker, Units.MineralField450);
        _unitsTracker.SetUnits(new List<Unit> { townHall, closeMineral });

        // Act
        var townHallSupervisor = new Bot.Managers.EconomyManagement.TownHallSupervision.TownHallSupervisor(_unitsTracker, _buildingTracker, _expandAnalyzerMock.Object, townHall, Colors.Cyan);

        // Assert
        Assert.Equal(2, townHallSupervisor.IdealCapacity);
        Assert.Equal(3, townHallSupervisor.SaturatedCapacity);
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenFarGas_WhenNewTownHallSupervisor_ThenDoesNotAssignGas() {
        // Arrange
        var townHall = TestUtils.CreateUnit(_unitsTracker, Units.Hatchery);
        var farGasGeyser = TestUtils.CreateUnit(_unitsTracker, Units.SpacePlatformGeyser, vespeneContents: 100, position: new Vector3(10, 10, 10));
        _unitsTracker.SetUnits(new List<Unit> { townHall, farGasGeyser });

        // Act
        var townHallSupervisor = new Bot.Managers.EconomyManagement.TownHallSupervision.TownHallSupervisor(_unitsTracker, _buildingTracker, _expandAnalyzerMock.Object, townHall, Colors.Cyan);

        // Assert
        Assert.Null(farGasGeyser.Supervisor);
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenCloseGas_WhenNewTownHallSupervisor_ThenAssignGas() {
        // Arrange
        var townHall = TestUtils.CreateUnit(_unitsTracker, Units.Hatchery);
        var closeGasGeyser = TestUtils.CreateUnit(_unitsTracker, Units.SpacePlatformGeyser, vespeneContents: 100);
        _unitsTracker.SetUnits(new List<Unit> { townHall, closeGasGeyser });

        // Act
        var townHallSupervisor = new Bot.Managers.EconomyManagement.TownHallSupervision.TownHallSupervisor(_unitsTracker, _buildingTracker, _expandAnalyzerMock.Object, townHall, Colors.Cyan);

        // Assert
        Assert.Equal(townHallSupervisor, closeGasGeyser.Supervisor);
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenGasAndExtractor_WhenNewTownHallSupervisor_ThenAssignExtractor() {
        // Arrange
        var townHall = TestUtils.CreateUnit(_unitsTracker, Units.Hatchery);
        var closeGasGeyser = TestUtils.CreateUnit(_unitsTracker, Units.SpacePlatformGeyser, vespeneContents: 100);
        var extractor = TestUtils.CreateUnit(_unitsTracker, Units.Extractor);
        _unitsTracker.SetUnits(new List<Unit> { townHall, closeGasGeyser, extractor });

        // Act
        var townHallSupervisor = new Bot.Managers.EconomyManagement.TownHallSupervision.TownHallSupervisor(_unitsTracker, _buildingTracker, _expandAnalyzerMock.Object, townHall, Colors.Cyan);

        // Assert
        Assert.Equal(townHallSupervisor, extractor.Supervisor);
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenGasAndExtractor_WhenGasDepletes_ThenReleasesGasAndExtractor() {
        // Arrange
        var townHall = TestUtils.CreateUnit(_unitsTracker, Units.Hatchery);
        var closeGasGeyser = TestUtils.CreateUnit(_unitsTracker, Units.SpacePlatformGeyser, vespeneContents: 100);
        var extractor = TestUtils.CreateUnit(_unitsTracker, Units.Extractor);
        _unitsTracker.SetUnits(new List<Unit> { townHall, closeGasGeyser, extractor });

        var townHallSupervisor = new Bot.Managers.EconomyManagement.TownHallSupervision.TownHallSupervisor(_unitsTracker, _buildingTracker, _expandAnalyzerMock.Object, townHall, Colors.Cyan);

        // Act
        closeGasGeyser.RawUnitData.VespeneContents = 0;
        closeGasGeyser.Update(closeGasGeyser.RawUnitData, frame: 1);
        townHallSupervisor.OnFrame();

        // Assert
        Assert.Null(closeGasGeyser.Supervisor);
        Assert.Null(extractor.Supervisor);
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenTownHallAndMineral_WhenAssigningWorker_ThenAvailableCapacityDecreases() {
        // Arrange
        var townHall = TestUtils.CreateUnit(_unitsTracker, Units.Hatchery);
        var closeMineral = TestUtils.CreateUnit(_unitsTracker, Units.MineralField450);
        var worker = TestUtils.CreateUnit(_unitsTracker, Units.Drone);
        _unitsTracker.SetUnits(new List<Unit> { townHall, closeMineral, worker });

        // Act
        var townHallSupervisor = new Bot.Managers.EconomyManagement.TownHallSupervision.TownHallSupervisor(_unitsTracker, _buildingTracker, _expandAnalyzerMock.Object, townHall, Colors.Cyan);
        townHallSupervisor.Assign(worker);
        townHallSupervisor.OnFrame();

        // Assert
        Assert.Equal(1, townHallSupervisor.IdealAvailableCapacity);
        Assert.Equal(2, townHallSupervisor.SaturatedAvailableCapacity);
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenTownHallWorkerAndMineral_WhenMineralDies_ThenReleasesMineral() {
        // Arrange
        var townHall = TestUtils.CreateUnit(_unitsTracker, Units.Hatchery);
        var closeMineral = TestUtils.CreateUnit(_unitsTracker, Units.MineralField450);
        var worker = TestUtils.CreateUnit(_unitsTracker, Units.Drone);
        _unitsTracker.SetUnits(new List<Unit> { townHall, closeMineral, worker });

        var townHallSupervisor = new Bot.Managers.EconomyManagement.TownHallSupervision.TownHallSupervisor(_unitsTracker, _buildingTracker, _expandAnalyzerMock.Object, townHall, Colors.Cyan);
        townHallSupervisor.Assign(worker);
        townHallSupervisor.OnFrame();

        // Act
        closeMineral.Died();

        // Assert
        Assert.Null(closeMineral.Supervisor);
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenTownHallWorkerAndMineral_WhenMineralDies_ThenReleasesWorker() {
        // Arrange
        var townHall = TestUtils.CreateUnit(_unitsTracker, Units.Hatchery);
        var closeMineral = TestUtils.CreateUnit(_unitsTracker, Units.MineralField450);
        var worker = TestUtils.CreateUnit(_unitsTracker, Units.Drone);
        _unitsTracker.SetUnits(new List<Unit> { townHall, closeMineral, worker });

        var townHallSupervisor = new Bot.Managers.EconomyManagement.TownHallSupervision.TownHallSupervisor(_unitsTracker, _buildingTracker, _expandAnalyzerMock.Object, townHall, Colors.Cyan);
        townHallSupervisor.Assign(worker);
        townHallSupervisor.OnFrame();

        // Act
        closeMineral.Died();
        townHallSupervisor.OnFrame();

        // Assert
        Assert.Null(worker.Supervisor);
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenTownHallWorkerMineralGasAndExtractor_WhenRetire_ThenReleaseEverything() {
        // Arrange
        var townHall = TestUtils.CreateUnit(_unitsTracker, Units.Hatchery);
        var closeMineral = TestUtils.CreateUnit(_unitsTracker, Units.MineralField450);
        var closeGas = TestUtils.CreateUnit(_unitsTracker, Units.SpacePlatformGeyser, vespeneContents: 100);
        var extractor = TestUtils.CreateUnit(_unitsTracker, Units.Extractor);
        var worker = TestUtils.CreateUnit(_unitsTracker, Units.Drone);
        _unitsTracker.SetUnits(new List<Unit> { townHall, closeMineral, closeGas, extractor, worker });

        var townHallSupervisor = new Bot.Managers.EconomyManagement.TownHallSupervision.TownHallSupervisor(_unitsTracker, _buildingTracker, _expandAnalyzerMock.Object, townHall, Colors.Cyan);
        townHallSupervisor.Assign(worker);
        townHallSupervisor.OnFrame();

        // Act
        townHallSupervisor.Retire();

        // Assert
        Assert.Null(worker.Supervisor);
        Assert.Null(closeMineral.Supervisor);
        Assert.Null(closeGas.Supervisor);
        Assert.Null(extractor.Supervisor);
    }
}
