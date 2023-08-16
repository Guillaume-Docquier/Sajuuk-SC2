using Sajuuk.Actions;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.Tagging;
using Sajuuk.Tests.Mocks;
using Moq;
using SC2APIProtocol;

namespace Sajuuk.Tests.GameSense;

public class EnemyRaceTrackerTests : BaseTestClass {
    private readonly Mock<IFrameClock> _frameClockMock;
    private readonly IActionBuilder _actionBuilder;
    private readonly Mock<IActionService> _actionServiceMock;
    private readonly Mock<ITerrainTracker> _terrainTrackerMock;
    private readonly Mock<IRegionsTracker> _regionsTrackerMock;
    private readonly TestUnitsTracker _unitsTracker;
    private readonly Mock<ITaggingService> _taggingServiceMock;

    public EnemyRaceTrackerTests() {
        _frameClockMock = new Mock<IFrameClock>();
        _actionBuilder = new ActionBuilder(KnowledgeBase);
        _actionServiceMock = new Mock<IActionService>();
        _terrainTrackerMock = new Mock<ITerrainTracker>();
        _regionsTrackerMock = new Mock<IRegionsTracker>();
        _unitsTracker = new TestUnitsTracker();
        _taggingServiceMock = new Mock<ITaggingService>();
    }

    [Theory]
    [InlineData(Race.Zerg, Race.Random)]
    [InlineData(Race.Terran, Race.Zerg)]
    [InlineData(Race.Protoss, Race.Terran)]
    [InlineData(Race.Random, Race.Protoss)]
    public void GivenPlayersWithDistinctRaces_WhenUpdate_SetsEnemyRace(Race playerRace, Race enemyRace) {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: playerRace, enemyRace: enemyRace);
        var observation = ResponseGameObservationUtils.CreateResponseObservation(Enumerable.Empty<Unit>());

        var enemyRaceTracker = new EnemyRaceTracker(_taggingServiceMock.Object, _unitsTracker);

        // Act
        enemyRaceTracker.Update(observation, gameInfo);

        // Assert
        Assert.Equal(enemyRace, enemyRaceTracker.EnemyRace);
    }

    [Theory]
    [InlineData(Race.Terran, Race.Terran)]
    [InlineData(Race.Protoss, Race.Protoss)]
    [InlineData(Race.Zerg, Race.Zerg)]
    [InlineData(Race.Random, Race.Random)]
    public void GivenPlayersWithSameRaces_WhenUpdate_SetsEnemyRace(Race playerRace, Race enemyRace) {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: playerRace, enemyRace: enemyRace);
        var observation = ResponseGameObservationUtils.CreateResponseObservation(Enumerable.Empty<SC2APIProtocol.Unit>());

        var enemyRaceTracker = new EnemyRaceTracker(_taggingServiceMock.Object, _unitsTracker);

        // Act
        enemyRaceTracker.Update(observation, gameInfo);

        // Assert
        Assert.Equal(enemyRace, enemyRaceTracker.EnemyRace);
    }

    [Fact]
    public void GivenEnemyRandomRaceAndNoVisibleUnits_WhenUpdate_ThenRaceDoesntChange() {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: Race.Zerg, enemyRace: Race.Random);
        var observation = ResponseGameObservationUtils.CreateResponseObservation(Enumerable.Empty<SC2APIProtocol.Unit>());

        var enemyRaceTracker = new EnemyRaceTracker(_taggingServiceMock.Object, _unitsTracker);
        enemyRaceTracker.Update(observation, gameInfo); // Race.Random

        // Act
        enemyRaceTracker.Update(observation, gameInfo);

        // Assert
        Assert.Equal(Race.Random, enemyRaceTracker.EnemyRace);
    }

    public static IEnumerable<object[]> EnemyRandomRaceAndVisibleUnitsTestData() {
        yield return new object[] { new List<SC2APIProtocol.Unit> { TestUtils.CreateUnitRaw(Units.Scv, alliance: Alliance.Enemy) }, Race.Terran };
        yield return new object[] { new List<SC2APIProtocol.Unit> { TestUtils.CreateUnitRaw(Units.Probe, alliance: Alliance.Enemy) }, Race.Protoss };
        yield return new object[] { new List<SC2APIProtocol.Unit> { TestUtils.CreateUnitRaw(Units.Drone, alliance: Alliance.Enemy) }, Race.Zerg };
    }

    [Theory]
    [MemberData(nameof(EnemyRandomRaceAndVisibleUnitsTestData))]
    public void GivenRandomPlayerAndVisibleUnits_WhenUpdate_ThenResolvesEnemyRace(List<SC2APIProtocol.Unit> rawUnits, Race expectedRace) {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: Race.Zerg, enemyRace: Race.Random);
        var observation = ResponseGameObservationUtils.CreateResponseObservation(Enumerable.Empty<SC2APIProtocol.Unit>());

        var enemyRaceTracker = new EnemyRaceTracker(_taggingServiceMock.Object, _unitsTracker);

        var units = rawUnits.Select(rawUnit => new Unit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, rawUnit, currentFrame: 0)).ToList();
        _unitsTracker.SetUnits(units);

        // Act
        enemyRaceTracker.Update(observation, gameInfo);

        // Assert
        Assert.Equal(expectedRace, enemyRaceTracker.EnemyRace);
    }

    [Theory]
    [MemberData(nameof(EnemyRandomRaceAndVisibleUnitsTestData))]
    public void GivenEnemyRandomRaceAndVisibleUnits_WhenUpdate_ThenResolvesEnemyRace(List<SC2APIProtocol.Unit> rawUnits, Race expectedRace) {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: Race.Zerg, enemyRace: Race.Random);
        var observation = ResponseGameObservationUtils.CreateResponseObservation(Enumerable.Empty<Unit>());

        var enemyRaceTracker = new EnemyRaceTracker(_taggingServiceMock.Object, _unitsTracker);
        enemyRaceTracker.Update(observation, gameInfo); // Race.Random

        var units = rawUnits.Select(rawUnit => new Unit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, rawUnit, currentFrame: 0)).ToList();
        _unitsTracker.SetUnits(units);

        // Act
        enemyRaceTracker.Update(observation, gameInfo);

        // Assert
        Assert.Equal(expectedRace, enemyRaceTracker.EnemyRace);
    }

    [Theory]
    [MemberData(nameof(EnemyRandomRaceAndVisibleUnitsTestData))]
    public void GivenEnemyResolvedRaceAndNoVisibleUnits_WhenUpdate_ThenRaceDoesntChange(List<SC2APIProtocol.Unit> rawUnits, Race expectedRace) {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: Race.Zerg, enemyRace: Race.Random);
        var observation = ResponseGameObservationUtils.CreateResponseObservation(Enumerable.Empty<SC2APIProtocol.Unit>(), frame: 0);

        var enemyRaceTracker = new EnemyRaceTracker(_taggingServiceMock.Object, _unitsTracker);
        enemyRaceTracker.Update(observation, gameInfo); // Race.Random

        var units = rawUnits.Select(rawUnit => new Unit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, rawUnit, currentFrame: 0)).ToList();
        _unitsTracker.SetUnits(units);
        enemyRaceTracker.Update(observation, gameInfo); // expectedRace

        _unitsTracker.SetUnits(new List<Unit>());

        // Act
        enemyRaceTracker.Update(observation, gameInfo);

        // Assert
        Assert.Equal(expectedRace, enemyRaceTracker.EnemyRace);
    }

    [Theory]
    [InlineData(Race.Terran, Race.Terran)]
    [InlineData(Race.Protoss, Race.Protoss)]
    [InlineData(Race.Zerg, Race.Zerg)]
    [InlineData(Race.Random, Race.Random)]
    public void GivenPlayerRaces_WhenUpdate_TagsEnemyRaceOnce(Race playerRace, Race enemyRace) {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: playerRace, enemyRace: enemyRace);
        var observation = ResponseGameObservationUtils.CreateResponseObservation(Enumerable.Empty<SC2APIProtocol.Unit>());

        var enemyRaceTracker = new EnemyRaceTracker(_taggingServiceMock.Object, _unitsTracker);

        // Act
        enemyRaceTracker.Update(observation, gameInfo);

        // Assert
        _taggingServiceMock.Verify(taggingService => taggingService.TagEnemyRace(It.IsAny<Race>()), Times.Once);
        _taggingServiceMock.Verify(taggingService => taggingService.TagEnemyRace(enemyRace), Times.Once);
    }

    [Theory]
    [InlineData(Race.Terran, Race.Terran)]
    [InlineData(Race.Protoss, Race.Protoss)]
    [InlineData(Race.Zerg, Race.Zerg)]
    [InlineData(Race.Random, Race.Random)]
    public void GivenEnemyRace_WhenFurtherUpdates_TagsNothing(Race playerRace, Race enemyRace) {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: playerRace, enemyRace: enemyRace);
        var observation = ResponseGameObservationUtils.CreateResponseObservation(Enumerable.Empty<SC2APIProtocol.Unit>());

        var enemyRaceTracker = new EnemyRaceTracker(_taggingServiceMock.Object, _unitsTracker);
        enemyRaceTracker.Update(observation, gameInfo); // Race.Random

        // Act
        _taggingServiceMock.Reset();
        enemyRaceTracker.Update(observation, gameInfo);
        enemyRaceTracker.Update(observation, gameInfo);
        enemyRaceTracker.Update(observation, gameInfo);

        // Assert
        _taggingServiceMock.Verify(taggingService => taggingService.TagEnemyRace(It.IsAny<Race>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(EnemyRandomRaceAndVisibleUnitsTestData))]
    public void GivenEnemyRandomRaceAndVisibleUnits_WhenUpdate_ThenTagsActualRaceOnce(List<SC2APIProtocol.Unit> rawUnits, Race expectedRace) {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: Race.Zerg, enemyRace: Race.Random);
        var observation = ResponseGameObservationUtils.CreateResponseObservation(Enumerable.Empty<Unit>());

        var enemyRaceTracker = new EnemyRaceTracker(_taggingServiceMock.Object, _unitsTracker);
        enemyRaceTracker.Update(observation, gameInfo); // Race.Random

        _taggingServiceMock.Reset();
        var units = rawUnits.Select(rawUnit => new Unit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, rawUnit, currentFrame: 0)).ToList();
        _unitsTracker.SetUnits(units);

        // Act
        enemyRaceTracker.Update(observation, gameInfo);

        // Assert
        _taggingServiceMock.Verify(taggingService => taggingService.TagEnemyRace(It.IsAny<Race>()), Times.Once);
        _taggingServiceMock.Verify(taggingService => taggingService.TagEnemyRace(expectedRace), Times.Once);
    }

    [Theory]
    [MemberData(nameof(EnemyRandomRaceAndVisibleUnitsTestData))]
    public void GivenEnemyResolvedRandomRaceAndVisibleUnits_WhenFurtherUpdates_ThenTagsNothing(List<SC2APIProtocol.Unit> rawUnits, Race expectedRace) {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: Race.Zerg, enemyRace: Race.Random);
        var observation = ResponseGameObservationUtils.CreateResponseObservation(Enumerable.Empty<Unit>());

        var enemyRaceTracker = new EnemyRaceTracker(_taggingServiceMock.Object, _unitsTracker);
        enemyRaceTracker.Update(observation, gameInfo); // Race.Random

        var units = rawUnits.Select(rawUnit => new Unit(_frameClockMock.Object, KnowledgeBase, _actionBuilder, _actionServiceMock.Object, _terrainTrackerMock.Object, _regionsTrackerMock.Object, _unitsTracker, rawUnit, 0)).ToList();
        _unitsTracker.SetUnits(units);
        enemyRaceTracker.Update(observation, gameInfo); // expectedRace

        _taggingServiceMock.Reset();

        // Act
        enemyRaceTracker.Update(observation, gameInfo);
        enemyRaceTracker.Update(observation, gameInfo);
        enemyRaceTracker.Update(observation, gameInfo);

        // Assert
        _taggingServiceMock.Verify(taggingService => taggingService.TagEnemyRace(It.IsAny<Race>()), Times.Never);
    }

    [Fact]
    public void GivenEnemyRace_WhenReset_ThenRaceIsNoRace() {
        // Arrange
        var gameInfo = ResponseGameInfoUtils.CreateResponseGameInfo(playerRace: Race.Zerg, enemyRace: Race.Terran);
        var observation = ResponseGameObservationUtils.CreateResponseObservation(Enumerable.Empty<SC2APIProtocol.Unit>());

        var enemyRaceTracker = new EnemyRaceTracker(_taggingServiceMock.Object, _unitsTracker);
        enemyRaceTracker.Update(observation, gameInfo); // Race.Terran

        // Act
        enemyRaceTracker.Reset();

        // Assert
        Assert.Equal(Race.NoRace, enemyRaceTracker.EnemyRace);
    }
}
