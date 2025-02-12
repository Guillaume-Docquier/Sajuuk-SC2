using FluentAssertions;
using MapAnalysis.RegionAnalysis.Persistence;
using MapAnalysis.RegionAnalysis.Ramps;
using SC2Client;
using SC2Client.Debugging.Images;
using SC2Client.Logging;
using SC2Client.State;
using SC2Client.Trackers;

namespace MapAnalysis.Tests.RegionAnalysis.Ramps;

// TODO GD Create the test data, make GameState and Ramp serializable with just [Serializable] !? Or [JsonInclude]?
public class RampFinderTests {
    private class RampFinderTestsData {
        public GameState InitialGameState { get; private set; }
        public List<Ramp> ExpectedRamps { get; private set; }
    }

    public static IEnumerable<object[]> AllMaps() { return Maps.GetAll().Except(new[] { Maps.Blackburn }).Select(map => new object[] { map }); }

    [Theory]
    [MemberData(nameof(AllMaps))]
    public void ShouldFindRampsOnKnownMaps(string mapFileName) {
        // Arrange
        var logger = new NoLogger();
        var mapFileNameFormatter = new MapFileNameFormatter(".");
        var jsonRepository = new JsonMapDataRepository<RampFinderTestsData>(logger);

        var testData = jsonRepository.Load(mapFileNameFormatter.Format("RampFinderTests", mapFileName));
        Assert.True(testData != null, "No test data found for this map. Did you forget to setup test data?");

        var terrainTracker = new TerrainTracker(logger);
        var mapImageFactory = new NoMapImageFactory();
        var rampFinder = new RampFinder(terrainTracker, mapImageFactory, mapFileNameFormatter, logger, mapFileName);

        terrainTracker.Update(testData.InitialGameState);

        // Act
        var ramps = rampFinder.FindRamps(terrainTracker.Cells);

        // Assert
        ramps.Should().BeEquivalentTo(testData.ExpectedRamps);
    }
}
