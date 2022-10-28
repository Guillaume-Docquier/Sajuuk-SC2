using Bot.GameData;
using Bot.Tests.Fixtures;
using SC2APIProtocol;

namespace Bot.Tests;

[Collection("Sequential")]
public class ControllerTests : IClassFixture<KnowledgeBaseFixture>, IClassFixture<GraphicalDebuggerFixture>, IDisposable {
    public ControllerTests() {
        Controller.Reset();
    }

    public void Dispose() {
        Controller.Reset();
    }

    [Theory]
    [InlineData(Race.Zerg, Race.Terran)]
    [InlineData(Race.Terran, Race.Protoss)]
    [InlineData(Race.Protoss, Race.Zerg)]
    public void GivenPlayersWithDistinctRaces_WhenNewGameInfo_SetsEnemyRace(Race playerRace, Race enemyRace) {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: playerRace, enemyRace: enemyRace);
        var observation = ResponseGameObservationUtils.CreateResponseObservation(units: BaseTestClass.GetInitialUnits());

        // Act
        Controller.NewFrame(gameInfo, observation);

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
        var observation = ResponseGameObservationUtils.CreateResponseObservation(units: BaseTestClass.GetInitialUnits());

        // Act
        Controller.NewFrame(gameInfo, observation);

        // Assert
        Assert.Equal(enemyRace, Controller.EnemyRace);
    }

    [Fact]
    public void GivenEnemyRandomRaceAndNoVisibleUnits_WhenNewGameInfo_ThenSetsRaceToRandom() {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: Race.Zerg, enemyRace: Race.Random);
        var observation = ResponseGameObservationUtils.CreateResponseObservation(units: BaseTestClass.GetInitialUnits());

        // Act
        Controller.NewFrame(gameInfo, observation);

        // Assert
        Assert.Equal(Race.Random, Controller.EnemyRace);
    }

    public static IEnumerable<object[]> EnemyRandomRaceAndVisibleUnitsTestData() {
        var units = BaseTestClass.GetInitialUnits();

        yield return new object[] { units.Concat(new List<Unit> { TestUtils.CreateUnit(Units.Scv, alliance: Alliance.Enemy) }), Race.Terran };
        yield return new object[] { units.Concat(new List<Unit> { TestUtils.CreateUnit(Units.Probe, alliance: Alliance.Enemy) }), Race.Protoss };
        yield return new object[] { units.Concat(new List<Unit> { TestUtils.CreateUnit(Units.Drone, alliance: Alliance.Enemy) }), Race.Zerg };
    }

    [Theory]
    [MemberData(nameof(EnemyRandomRaceAndVisibleUnitsTestData))]
    public void GivenEnemyRandomRaceAndVisibleUnits_WhenNewGameInfo_ThenResolvesEnemyRace(IEnumerable<Unit> units, Race expectedRace) {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: Race.Zerg, enemyRace: Race.Random);
        var observation = ResponseGameObservationUtils.CreateResponseObservation(units: units);

        // Because of dependencies issues, the random race resolver only works after the initial update
        // TODO GD Make the race resolver a Tracker instead of being in the controller
        Controller.NewFrame(gameInfo, observation);

        // Act
        Controller.NewFrame(gameInfo, observation);

        // Assert
        Assert.Equal(expectedRace, Controller.EnemyRace);
    }
}
