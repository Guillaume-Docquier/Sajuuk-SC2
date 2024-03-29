﻿using System.Numerics;
using Sajuuk.Actions;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.Managers.EconomyManagement.TownHallSupervision;
using Sajuuk.Tests.Mocks;
using Sajuuk.UnitModules;
using Moq;
using Sajuuk.Builds.BuildRequests;
using SC2APIProtocol;

namespace Sajuuk.Tests.Managers.EconomyManagement.TownHallSupervision;

public class TownHallSupervisorTests : BaseTestClass {
    private readonly TestUnitsTracker _unitsTracker;
    private readonly Mock<IRegionsTracker> _regionsTrackerMock;
    private readonly Mock<IBuildRequestFactory> _buildRequestFactoryMock;
    private readonly Mock<IGraphicalDebugger> _graphicalDebuggerMock;
    private readonly Mock<IFrameClock> _frameClockMock;
    private readonly Mock<IUnitModuleInstaller> _unitModuleInstallerMock;
    private readonly IActionBuilder _actionBuilder;
    private readonly Mock<IActionService> _actionServiceMock;
    private readonly Mock<ITerrainTracker> _terrainTrackerMock;

    public TownHallSupervisorTests() {
        _unitsTracker = new TestUnitsTracker();
        _regionsTrackerMock = new Mock<IRegionsTracker>();
        _buildRequestFactoryMock = new Mock<IBuildRequestFactory>();
        _graphicalDebuggerMock = new Mock<IGraphicalDebugger>();
        _frameClockMock = new Mock<IFrameClock>();
        _unitModuleInstallerMock = new Mock<IUnitModuleInstaller>();
        _actionBuilder = new ActionBuilder(KnowledgeBase);
        _actionServiceMock = new Mock<IActionService>();
        _terrainTrackerMock = new Mock<ITerrainTracker>();
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenFarMineral_WhenNewTownHallSupervisor_ThenDoesNotAssignMineral() {
        // Arrange
        var townHall = CreateUnit(Units.Hatchery);
        var farMineral = CreateUnit(Units.MineralField450, position: new Vector3(10, 10, 10));
        _unitsTracker.SetUnits(new List<Unit> { townHall, farMineral });

        // Act
        var townHallSupervisor = new TownHallSupervisor(_unitsTracker, _buildRequestFactoryMock.Object, _graphicalDebuggerMock.Object, _frameClockMock.Object, _unitModuleInstallerMock.Object, townHall, Colors.Cyan);

        // Assert
        Assert.Null(farMineral.Supervisor);
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenCloseMineral_WhenNewTownHallSupervisor_ThenAssignMineral() {
        // Arrange
        var townHall = CreateUnit(Units.Hatchery);
        var closeMineral = CreateUnit(Units.MineralField450);
        _unitsTracker.SetUnits(new List<Unit> { townHall, closeMineral });

        // Act
        var townHallSupervisor = new TownHallSupervisor(_unitsTracker, _buildRequestFactoryMock.Object, _graphicalDebuggerMock.Object, _frameClockMock.Object, _unitModuleInstallerMock.Object, townHall, Colors.Cyan);

        // Assert
        Assert.Equal(townHallSupervisor, closeMineral.Supervisor);
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenTownHallAndMineral_WhenNewTownHallSupervisor_ThenCapacityIsSet() {
        // Arrange
        var townHall = CreateUnit(Units.Hatchery);
        var closeMineral = CreateUnit(Units.MineralField450);
        _unitsTracker.SetUnits(new List<Unit> { townHall, closeMineral });

        // Act
        var townHallSupervisor = new TownHallSupervisor(_unitsTracker, _buildRequestFactoryMock.Object, _graphicalDebuggerMock.Object, _frameClockMock.Object, _unitModuleInstallerMock.Object, townHall, Colors.Cyan);

        // Assert
        Assert.Equal(2, townHallSupervisor.IdealCapacity);
        Assert.Equal(3, townHallSupervisor.SaturatedCapacity);
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenFarGas_WhenNewTownHallSupervisor_ThenDoesNotAssignGas() {
        // Arrange
        var townHall = CreateUnit(Units.Hatchery);
        var farGasGeyser = CreateUnit(Units.SpacePlatformGeyser, position: new Vector3(10, 10, 10), vespeneContents: 100);
        _unitsTracker.SetUnits(new List<Unit> { townHall, farGasGeyser });

        // Act
        var townHallSupervisor = new TownHallSupervisor(_unitsTracker, _buildRequestFactoryMock.Object, _graphicalDebuggerMock.Object, _frameClockMock.Object, _unitModuleInstallerMock.Object, townHall, Colors.Cyan);

        // Assert
        Assert.Null(farGasGeyser.Supervisor);
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenCloseGas_WhenNewTownHallSupervisor_ThenAssignGas() {
        // Arrange
        var townHall = CreateUnit(Units.Hatchery);
        var closeGasGeyser = CreateUnit(Units.SpacePlatformGeyser, vespeneContents: 100);
        _unitsTracker.SetUnits(new List<Unit> { townHall, closeGasGeyser });

        // Act
        var townHallSupervisor = new TownHallSupervisor(_unitsTracker, _buildRequestFactoryMock.Object, _graphicalDebuggerMock.Object, _frameClockMock.Object, _unitModuleInstallerMock.Object, townHall, Colors.Cyan);

        // Assert
        Assert.Equal(townHallSupervisor, closeGasGeyser.Supervisor);
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenGasAndExtractor_WhenNewTownHallSupervisor_ThenAssignExtractor() {
        // Arrange
        var townHall = CreateUnit(Units.Hatchery);
        var closeGasGeyser = CreateUnit(Units.SpacePlatformGeyser, vespeneContents: 100);
        var extractor = CreateUnit(Units.Extractor);
        _unitsTracker.SetUnits(new List<Unit> { townHall, closeGasGeyser, extractor });

        // Act
        var townHallSupervisor = new TownHallSupervisor(_unitsTracker, _buildRequestFactoryMock.Object, _graphicalDebuggerMock.Object, _frameClockMock.Object, _unitModuleInstallerMock.Object, townHall, Colors.Cyan);

        // Assert
        Assert.Equal(townHallSupervisor, extractor.Supervisor);
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenGasAndExtractor_WhenGasDepletes_ThenReleasesGasAndExtractor() {
        // Arrange
        var townHall = CreateUnit(Units.Hatchery);
        var closeGasGeyser = CreateUnit(Units.SpacePlatformGeyser, vespeneContents: 100);
        var extractor = CreateUnit(Units.Extractor);
        _unitsTracker.SetUnits(new List<Unit> { townHall, closeGasGeyser, extractor });

        var townHallSupervisor = new TownHallSupervisor(_unitsTracker, _buildRequestFactoryMock.Object, _graphicalDebuggerMock.Object, _frameClockMock.Object, _unitModuleInstallerMock.Object, townHall, Colors.Cyan);

        // Act
        closeGasGeyser.RawUnitData.VespeneContents = 0;
        closeGasGeyser.Update(closeGasGeyser.RawUnitData, currentFrame: 1);
        townHallSupervisor.OnFrame();

        // Assert
        Assert.Null(closeGasGeyser.Supervisor);
        Assert.Null(extractor.Supervisor);
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenTownHallAndMineral_WhenAssigningWorker_ThenAvailableCapacityDecreases() {
        // Arrange
        var townHall = CreateUnit(Units.Hatchery);
        var closeMineral = CreateUnit(Units.MineralField450);
        var worker = CreateUnit(Units.Drone);
        _unitsTracker.SetUnits(new List<Unit> { townHall, closeMineral, worker });

        // Act
        var townHallSupervisor = new TownHallSupervisor(_unitsTracker, _buildRequestFactoryMock.Object, _graphicalDebuggerMock.Object, _frameClockMock.Object, _unitModuleInstallerMock.Object, townHall, Colors.Cyan);
        townHallSupervisor.Assign(worker);
        townHallSupervisor.OnFrame();

        // Assert
        Assert.Equal(1, townHallSupervisor.IdealAvailableCapacity);
        Assert.Equal(2, townHallSupervisor.SaturatedAvailableCapacity);
    }

    [Fact(Skip = "Wait for DI refactor to be done")]
    public void GivenTownHallWorkerAndMineral_WhenMineralDies_ThenReleasesMineral() {
        // Arrange
        var townHall = CreateUnit(Units.Hatchery);
        var closeMineral = CreateUnit(Units.MineralField450);
        var worker = CreateUnit(Units.Drone);
        _unitsTracker.SetUnits(new List<Unit> { townHall, closeMineral, worker });

        var townHallSupervisor = new TownHallSupervisor(_unitsTracker, _buildRequestFactoryMock.Object, _graphicalDebuggerMock.Object, _frameClockMock.Object, _unitModuleInstallerMock.Object, townHall, Colors.Cyan);
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
        var townHall = CreateUnit(Units.Hatchery);
        var closeMineral = CreateUnit(Units.MineralField450);
        var worker = CreateUnit(Units.Drone);
        _unitsTracker.SetUnits(new List<Unit> { townHall, closeMineral, worker });

        var townHallSupervisor = new TownHallSupervisor(_unitsTracker, _buildRequestFactoryMock.Object, _graphicalDebuggerMock.Object, _frameClockMock.Object, _unitModuleInstallerMock.Object, townHall, Colors.Cyan);
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
        var townHall = CreateUnit(Units.Hatchery);
        var closeMineral = CreateUnit(Units.MineralField450);
        var closeGas = CreateUnit(Units.SpacePlatformGeyser, vespeneContents: 100);
        var extractor = CreateUnit(Units.Extractor);
        var worker = CreateUnit(Units.Drone);
        _unitsTracker.SetUnits(new List<Unit> { townHall, closeMineral, closeGas, extractor, worker });

        var townHallSupervisor = new TownHallSupervisor(_unitsTracker, _buildRequestFactoryMock.Object, _graphicalDebuggerMock.Object, _frameClockMock.Object, _unitModuleInstallerMock.Object, townHall, Colors.Cyan);
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
