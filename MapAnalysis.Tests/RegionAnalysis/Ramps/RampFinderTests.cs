using FluentAssertions;
using MapAnalysis.RegionAnalysis;
using MapAnalysis.RegionAnalysis.Persistence;
using MapAnalysis.RegionAnalysis.Ramps;
using SC2Client;
using SC2Client.Debugging.Images;
using SC2Client.Logging;
using SC2Client.Trackers;

namespace MapAnalysis.Tests.RegionAnalysis.Ramps;

public class RampFinderTests {
    public static IEnumerable<object[]> AllMaps() { return Maps.GetAll().Except(new[] { Maps.Blackburn }).Select(map => new object[] { map }); }

    [Theory]
    [MemberData(nameof(AllMaps))]
    public void ShouldFindRampsOnKnownMaps(string mapFileName) {
        // Arrange
        var logger = new NoLogger();

        // Not ideal but I couldn't find a way not to hardcode the path while having the json next to the test file
        var mapFileNameFormatter = new MapFileNameFormatter("./RegionAnalysis/Ramps");

        var jsonRepository = new JsonMapDataRepository<RampFinderValidationData>(logger);

        var testData = jsonRepository.Load(mapFileNameFormatter.Format(RampFinderValidationData.FilenameTopic, mapFileName));
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
