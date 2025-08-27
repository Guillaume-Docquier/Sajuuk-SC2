using System.Drawing;
using System.Numerics;
using System.Reflection;
using MapAnalysis.RegionAnalysis.ChokePoints;
using MapAnalysis.RegionAnalysis.Persistence;
using Moq;
using SC2Client.Debugging.GraphicalDebugging;
using SC2Client.Debugging.Images;
using SC2Client.Logging;
using SC2Client.Trackers;

namespace MapAnalysis.Tests.RegionAnalysis.ChokePoints;

public class RayCastingChokeFinderTests
{
    private List<VisionLine> CreateLinesAtAnAngle(IChokeFinder chokeFinder, int mapSize, int angle)
    {
        var method = typeof(RayCastingChokeFinder).GetMethod("CreateLinesAtAnAngle", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var result = method.Invoke(chokeFinder, new object[] { angle, mapSize, mapSize });
        Assert.NotNull(result);

        return (List<VisionLine>)result;
    }

    private List<VisionLine> BreakDownIntoContinuousSegments(IChokeFinder chokeFinder, List<VisionLine> lines)
    {
        var method = typeof(RayCastingChokeFinder).GetMethod("BreakDownIntoContinuousSegments", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(IEnumerable<VisionLine>) }, null);
        Assert.NotNull(method);

        var result = method.Invoke(chokeFinder, new object[] { lines });
        Assert.NotNull(result);

        return (List<VisionLine>)result;
    }

    public static IEnumerable<object[]> LinesAtAnAngleData() {
        for (int angle = 0; angle < 180; angle++) {
            for (int mapSize = 0; mapSize < 250; mapSize += 15) {
                yield return new object[] { angle, mapSize };
            }
        }
    }

    [Theory]
    [MemberData(nameof(LinesAtAnAngleData))]
    public void CreateLinesAtAnAngle_WithAngle_ShouldCoverAllCells(int angle, int mapSize)
    {
        // Arrange
        var terrainTrackerMock = new Mock<ITerrainTracker>();

        var walkableCells = new HashSet<Vector2>();
        for (var x = 0; x < mapSize; x++)
        {
            for (var y = 0; y < mapSize; y++)
            {
                walkableCells.Add(new Vector2(x, y));
            }
        }

        terrainTrackerMock.Setup(t => t.Cells).Returns(walkableCells);
        terrainTrackerMock.Setup(t => t.MaxX).Returns(mapSize);
        terrainTrackerMock.Setup(t => t.MaxY).Returns(mapSize);

        var chokeFinder = new RayCastingChokeFinder(
            new NoLogger(),
            terrainTrackerMock.Object,
            new NullGraphicalDebugger(),
            new NoMapImageFactory(),
            new MapFileNameFormatter("don't care"),
            "test_map"
        );

        var walkableCellsCount = terrainTrackerMock.Object.Cells.Count;

        // Act
        var rawLines = CreateLinesAtAnAngle(chokeFinder, mapSize, angle);
        var lines = BreakDownIntoContinuousSegments(chokeFinder, rawLines);
        var coveredCells = lines.SelectMany(line => line.OrderedTraversedCells).ToHashSet();

        // var mapImage = new MapImage(new NoLogger(), mapSize, mapSize);
        // mapImage.SetCellsColor(coveredCells, Color.Green);
        // mapImage.Save($"./CreateLinesAtAnAngle_WithAngle_ShouldCoverAllCells_{angle}");

        // Assert

        // The original lines should cover everything
        var uncoveredCellsRaw = terrainTrackerMock.Object.Cells.Except(rawLines.SelectMany(line => line.OrderedTraversedCells));
        Assert.Empty(uncoveredCellsRaw);

        // Breaking down should not change that
        var uncoveredCells = terrainTrackerMock.Object.Cells.Except(coveredCells);
        Assert.Empty(uncoveredCells);

        // If they cover all the cells and have the same count, then they contain exactly all the cells.
        Assert.Equal(walkableCellsCount, coveredCells.Count);
    }
}
