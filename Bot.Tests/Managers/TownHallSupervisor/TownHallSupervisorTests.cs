﻿using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.Tests.Managers.TownHallSupervisor;

public class TownHallSupervisorTests : BaseTestClass {
    [Fact]
    public void GivenFarMineral_WhenNewTownHallSupervisor_ThenDoesNotAssignMineral() {
        // Arrange
        var farMineralRaw = TestUtils.CreateUnitRaw(Units.MineralField450, position: new Vector3(10, 10, 10));
        var units = new List<SC2APIProtocol.Unit> { farMineralRaw };

        var observation = ResponseGameObservationUtils.CreateResponseObservation(units);
        Controller.NewObservation(observation);

        // Act
        var townHall = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).First();
        var townHallSupervisor = new Bot.Managers.TownHallSupervisor(townHall, Colors.Cyan);

        // Assert
        var farMineral = Controller.GetUnits(UnitsTracker.NeutralUnits, Units.MineralField450).First();

        Assert.Null(farMineral.Supervisor);
    }

    [Fact]
    public void GivenCloseMineral_WhenNewTownHallSupervisor_ThenAssignMineral() {
        // Arrange
        var closeMineralRaw = TestUtils.CreateUnitRaw(Units.MineralField450);
        var units = new List<SC2APIProtocol.Unit> { closeMineralRaw };

        var observation = ResponseGameObservationUtils.CreateResponseObservation(units);
        Controller.NewObservation(observation);

        // Act
        var townHall = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).First();
        var townHallSupervisor = new Bot.Managers.TownHallSupervisor(townHall, Colors.Cyan);

        // Assert
        var closeMineral = Controller.GetUnits(UnitsTracker.NeutralUnits, Units.MineralField450).First();

        Assert.Equal(townHallSupervisor, closeMineral.Supervisor);
    }

    [Fact]
    public void GivenTownHallAndMineral_WhenNewTownHallSupervisor_ThenCapacityIsSet() {
        // Arrange
        var mineralRaw = TestUtils.CreateUnitRaw(Units.MineralField450);
        var units = new List<SC2APIProtocol.Unit> { mineralRaw };

        var observation = ResponseGameObservationUtils.CreateResponseObservation(units);
        Controller.NewObservation(observation);

        // Act
        var townHall = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).First();
        var townHallSupervisor = new Bot.Managers.TownHallSupervisor(townHall, Colors.Cyan);

        // Assert
        Assert.Equal(2, townHallSupervisor.IdealCapacity);
        Assert.Equal(3, townHallSupervisor.SaturatedCapacity);
    }

    [Fact]
    public void GivenFarGas_WhenNewTownHallSupervisor_ThenDoesNotAssignGas() {
        // Arrange
        var farGasRaw = TestUtils.CreateUnitRaw(Units.SpacePlatformGeyser, vespeneContents: 100, position: new Vector3(10, 10, 10));
        var units = new List<SC2APIProtocol.Unit> { farGasRaw };

        var observation = ResponseGameObservationUtils.CreateResponseObservation(units);
        Controller.NewObservation(observation);

        // Act
        var townHall = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).First();
        var townHallSupervisor = new Bot.Managers.TownHallSupervisor(townHall, Colors.Cyan);

        // Assert
        var farGas = Controller.GetUnits(UnitsTracker.NeutralUnits, Units.SpacePlatformGeyser).First();

        Assert.Null(farGas.Supervisor);
    }

    [Fact]
    public void GivenCloseGas_WhenNewTownHallSupervisor_ThenAssignGas() {
        // Arrange
        var closeGasRaw = TestUtils.CreateUnitRaw(Units.SpacePlatformGeyser, vespeneContents: 100);
        var units = new List<SC2APIProtocol.Unit> { closeGasRaw };

        var observation = ResponseGameObservationUtils.CreateResponseObservation(units);
        Controller.NewObservation(observation);

        // Act
        var townHall = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).First();
        var townHallSupervisor = new Bot.Managers.TownHallSupervisor(townHall, Colors.Cyan);

        // Assert
        var closeGas = Controller.GetUnits(UnitsTracker.NeutralUnits, Units.SpacePlatformGeyser).First();

        Assert.Equal(townHallSupervisor, closeGas.Supervisor);
    }

    [Fact]
    public void GivenGasAndExtractor_WhenNewTownHallSupervisor_ThenAssignExtractor() {
        // Arrange
        var gasRaw = TestUtils.CreateUnitRaw(Units.SpacePlatformGeyser, vespeneContents: 100);
        var extractorRaw = TestUtils.CreateUnitRaw(Units.Extractor);
        var units = new List<SC2APIProtocol.Unit> { gasRaw, extractorRaw };

        var observation = ResponseGameObservationUtils.CreateResponseObservation(units);
        Controller.NewObservation(observation);

        // Act
        var townHall = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).First();
        var townHallSupervisor = new Bot.Managers.TownHallSupervisor(townHall, Colors.Cyan);

        // Assert
        var extractor = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Extractor).First();

        Assert.Equal(townHallSupervisor, extractor.Supervisor);
    }

    [Fact]
    public void GivenGasAndExtractor_WhenGasDepletes_ThenReleasesGasAndExtractor() {
        // Arrange
        var gasRaw = TestUtils.CreateUnitRaw(Units.SpacePlatformGeyser, vespeneContents: 100);
        var extractorRaw = TestUtils.CreateUnitRaw(Units.Extractor);
        var units = new List<SC2APIProtocol.Unit> { gasRaw, extractorRaw };

        var observation = ResponseGameObservationUtils.CreateResponseObservation(units);
        Controller.NewObservation(observation);

        var townHall = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).First();
        var townHallSupervisor = new Bot.Managers.TownHallSupervisor(townHall, Colors.Cyan);

        // Act
        gasRaw.VespeneContents = 0;
        observation = ResponseGameObservationUtils.CreateResponseObservation(units);
        Controller.NewObservation(observation);
        townHallSupervisor.OnFrame();

        // Assert
        var gas = Controller.GetUnits(UnitsTracker.NeutralUnits, Units.SpacePlatformGeyser).First();
        var extractor = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Extractor).First();

        Assert.Null(gas.Supervisor);
        Assert.Null(extractor.Supervisor);
    }

    [Fact]
    public void GivenTownHallAndMineral_WhenAssigningWorker_ThenAvailableCapacityDecreases() {
        // Arrange
        var mineralRaw = TestUtils.CreateUnitRaw(Units.MineralField450);
        var units = new List<SC2APIProtocol.Unit> { mineralRaw };

        var observation = ResponseGameObservationUtils.CreateResponseObservation(units);
        Controller.NewObservation(observation);

        var worker = TestUtils.CreateUnit(Units.Drone);

        // Act
        var townHall = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).First();
        var townHallSupervisor = new Bot.Managers.TownHallSupervisor(townHall, Colors.Cyan);
        townHallSupervisor.Assign(worker);
        townHallSupervisor.OnFrame();

        // Assert
        Assert.Equal(1, townHallSupervisor.IdealAvailableCapacity);
        Assert.Equal(2, townHallSupervisor.SaturatedAvailableCapacity);
    }

    [Fact]
    public void GivenTownHallWorkerAndMineral_WhenMineralDies_ThenReleasesMineral() {
        // Arrange
        var mineralRaw = TestUtils.CreateUnitRaw(Units.MineralField450);
        var units = new List<SC2APIProtocol.Unit> { mineralRaw };

        var observation = ResponseGameObservationUtils.CreateResponseObservation(units);
        Controller.NewObservation(observation);

        var worker = TestUtils.CreateUnit(Units.Drone);
        var townHall = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).First();
        var townHallSupervisor = new Bot.Managers.TownHallSupervisor(townHall, Colors.Cyan);
        townHallSupervisor.Assign(worker);
        townHallSupervisor.OnFrame();

        // Act
        var mineral = Controller.GetUnits(UnitsTracker.NeutralUnits, Units.MineralField450).First();
        mineral.Died();

        // Assert
        Assert.Null(mineral.Supervisor);
    }

    [Fact]
    public void GivenTownHallWorkerAndMineral_WhenMineralDies_ThenReleasesWorker() {
        // Arrange
        var mineralRaw = TestUtils.CreateUnitRaw(Units.MineralField450);
        var townHall = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).First();
        var units = new List<SC2APIProtocol.Unit> { mineralRaw, townHall.RawUnitData };

        var observation = ResponseGameObservationUtils.CreateResponseObservation(units, frame: 1);
        Controller.NewObservation(observation);

        var worker = TestUtils.CreateUnit(Units.Drone);
        var townHallSupervisor = new Bot.Managers.TownHallSupervisor(townHall, Colors.Cyan);
        townHallSupervisor.Assign(worker);
        townHallSupervisor.OnFrame();

        // Act
        var mineral = Controller.GetUnits(UnitsTracker.NeutralUnits, Units.MineralField450).First();
        mineral.Died();
        townHallSupervisor.OnFrame();

        // Assert
        Assert.Null(worker.Supervisor);
    }

    [Fact]
    public void GivenTownHallWorkerMineralGasAndExtractor_WhenRetire_ThenReleaseEverything() {
        // Arrange
        var mineralRaw = TestUtils.CreateUnitRaw(Units.MineralField450);
        var gasRaw = TestUtils.CreateUnitRaw(Units.SpacePlatformGeyser, vespeneContents: 100);
        var extractorRaw = TestUtils.CreateUnitRaw(Units.Extractor);
        var units = new List<SC2APIProtocol.Unit> { mineralRaw, gasRaw, extractorRaw };

        var observation = ResponseGameObservationUtils.CreateResponseObservation(units);
        Controller.NewObservation(observation);

        var townHall = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).First();
        var townHallSupervisor = new Bot.Managers.TownHallSupervisor(townHall, Colors.Cyan);

        var worker = TestUtils.CreateUnit(Units.Drone);
        townHallSupervisor.Assign(worker);
        townHallSupervisor.OnFrame();

        // Act
        townHallSupervisor.Retire();

        // Assert
        var mineral = Controller.GetUnits(UnitsTracker.NeutralUnits, Units.MineralField450).First();
        var gas = Controller.GetUnits(UnitsTracker.NeutralUnits, Units.SpacePlatformGeyser).First();
        var extractor = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Extractor).First();

        Assert.Null(worker.Supervisor);
        Assert.Null(mineral.Supervisor);
        Assert.Null(gas.Supervisor);
        Assert.Null(extractor.Supervisor);
    }
}