using Bot.GameData;
using Bot.GameSense;
using Bot.Tests.Fixtures;
using Bot.Tests.Mocks;
using SC2APIProtocol;

namespace Bot.Tests.GameSense;

public class EnemyRaceTrackerTests : IClassFixture<KnowledgeBaseFixture> {
    [Theory]
    [InlineData(Race.Zerg, Race.Terran)]
    [InlineData(Race.Terran, Race.Protoss)]
    [InlineData(Race.Protoss, Race.Zerg)]
    public void GivenPlayersWithDistinctRaces_WhenNewGameInfo_SetsEnemyRace(Race playerRace, Race enemyRace) {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: playerRace, enemyRace: enemyRace);
        var observation = ResponseGameObservationUtils.CreateResponseObservation(units: BaseTestClass.GetInitialUnits());
        var enemyRaceTracker = new EnemyRaceTracker(new DummyTaggingService());

        // Act
        enemyRaceTracker.Update(observation, gameInfo);

        // Assert
        Assert.Equal(enemyRace, enemyRaceTracker.EnemyRace);
    }

    [Theory]
    [InlineData(Race.Terran, Race.Terran)]
    [InlineData(Race.Protoss, Race.Protoss)]
    [InlineData(Race.Zerg, Race.Zerg)]
    public void GivenPlayersWithSameRaces_WhenNewGameInfo_SetsEnemyRace(Race playerRace, Race enemyRace) {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: playerRace, enemyRace: enemyRace);
        var observation = ResponseGameObservationUtils.CreateResponseObservation(units: BaseTestClass.GetInitialUnits());
        var enemyRaceTracker = new EnemyRaceTracker(new DummyTaggingService());

        // Act
        enemyRaceTracker.Update(observation, gameInfo);

        // Assert
        Assert.Equal(enemyRace, enemyRaceTracker.EnemyRace);
    }

    [Fact]
    public void GivenEnemyRandomRaceAndNoVisibleUnits_WhenNewGameInfo_ThenSetsRaceToRandom() {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: Race.Zerg, enemyRace: Race.Random);
        var observation = ResponseGameObservationUtils.CreateResponseObservation(units: BaseTestClass.GetInitialUnits());
        var enemyRaceTracker = new EnemyRaceTracker(new DummyTaggingService());

        // Act
        enemyRaceTracker.Update(observation, gameInfo);

        // Assert
        Assert.Equal(Race.Random, enemyRaceTracker.EnemyRace);
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
        UnitsTracker.Instance.Reset(); // TODO GD Review the test setup, right now the ResponseGameObservationUtils depend on the UnitsTracker.Instance
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: Race.Zerg, enemyRace: Race.Random);
        var observation = ResponseGameObservationUtils.CreateResponseObservation(units: units);

        var enemyRaceTracker = new EnemyRaceTracker(new DummyTaggingService());
        enemyRaceTracker.Update(observation, gameInfo);
        UnitsTracker.Instance.Update(observation, gameInfo);

        // Act
        enemyRaceTracker.Update(observation, gameInfo);

        // Assert
        Assert.Equal(expectedRace, enemyRaceTracker.EnemyRace);
    }
}
