using System.Numerics;
using Bot.GameData;
using Bot.Tests.Fixtures;
using SC2APIProtocol;

namespace Bot.Tests;

[Collection("Sequential")]
public class ControllerTests : IClassFixture<KnowledgeBaseFixture>, IDisposable {
    public ControllerTests() {
        Controller.Reset();
    }

    public void Dispose() {
        Controller.Reset();
    }

    [Fact]
    public void GivenNullObservation_WhenNewGameInfo_DoesNotSetEnemyRace() {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo();

        // Act
        Controller.NewGameInfo(gameInfo);

        // Assert
        Assert.Equal(Race.NoRace, Controller.EnemyRace);
    }

    [Theory]
    [InlineData(Race.Zerg, Race.Terran)]
    [InlineData(Race.Terran, Race.Protoss)]
    [InlineData(Race.Protoss, Race.Zerg)]
    public void GivenPlayersWithDistinctRaces_WhenNewGameInfo_SetsEnemyRace(Race playerRace, Race enemyRace) {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: playerRace, enemyRace: enemyRace);
        Controller.NewGameInfo(gameInfo);

        var startingTownHall = TestUtils.CreateUnit(Units.Hatchery, position: new Vector3(0, 0, 0));
        var observation = ResponseGameObservationUtils.CreateResponseObservation(units: new List<Unit> { startingTownHall });
        Controller.NewObservation(observation);

        // Act
        Controller.NewGameInfo(gameInfo);

        // Assert
        Assert.Equal(enemyRace, Controller.EnemyRace);
    }

    [Theory]
    [InlineData(Race.Terran, Race.Terran)]
    [InlineData(Race.Protoss, Race.Protoss)]
    [InlineData(Race.Zerg, Race.Zerg)]
    public void GivenPlayersWithSameRaces_WhenNewGameInfo_SetsEnemyRace(Race playerRace, Race enemyRace) {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: playerRace, enemyRace: enemyRace);
        Controller.NewGameInfo(gameInfo);

        var startingTownHall = TestUtils.CreateUnit(Units.Hatchery, position: new Vector3(0, 0, 0));
        var observation = ResponseGameObservationUtils.CreateResponseObservation(units: new List<Unit> { startingTownHall });
        Controller.NewObservation(observation);

        // Act
        Controller.NewGameInfo(gameInfo);

        // Assert
        Assert.Equal(enemyRace, Controller.EnemyRace);
    }

    [Fact]
    public void GivenEnemyRandomRaceAndNoVisibleUnits_WhenNewGameInfo_ThenSetsRaceToRandom() {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: Race.Zerg, enemyRace: Race.Random);
        Controller.NewGameInfo(gameInfo);

        var startingTownHall = TestUtils.CreateUnit(Units.Hatchery, position: new Vector3(0, 0, 0));
        var observation = ResponseGameObservationUtils.CreateResponseObservation(units: new List<Unit> { startingTownHall });
        Controller.NewObservation(observation);

        // Act
        Controller.NewGameInfo(gameInfo);

        // Assert
        Assert.Equal(Race.Random, Controller.EnemyRace);
    }

    public static IEnumerable<object[]> EnemyRandomRaceAndVisibleUnitsTestData() {
        var startingTownHall = TestUtils.CreateUnit(Units.Hatchery, position: new Vector3(0, 0, 0));

        yield return new object[] { new List<Unit> { startingTownHall, TestUtils.CreateUnit(Units.Scv, alliance: Alliance.Enemy) }, Race.Terran };
        yield return new object[] { new List<Unit> { startingTownHall, TestUtils.CreateUnit(Units.Probe, alliance: Alliance.Enemy) }, Race.Protoss };
        yield return new object[] { new List<Unit> { startingTownHall, TestUtils.CreateUnit(Units.Drone, alliance: Alliance.Enemy) }, Race.Zerg };
    }

    [Theory]
    [MemberData(nameof(EnemyRandomRaceAndVisibleUnitsTestData))]
    public void GivenEnemyRandomRaceAndVisibleUnits_WhenNewGameInfo_ThenResolvesEnemyRace(IEnumerable<Unit> units, Race expectedRace) {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: Race.Zerg, enemyRace: Race.Random);
        Controller.NewGameInfo(gameInfo);

        var observation = ResponseGameObservationUtils.CreateResponseObservation(units: units);
        Controller.NewObservation(observation);

        // Act
        Controller.NewGameInfo(gameInfo);

        // Assert
        Assert.Equal(expectedRace, Controller.EnemyRace);
    }
}
