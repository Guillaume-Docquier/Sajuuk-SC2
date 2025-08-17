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
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<ITerrainTracker> _terrainTrackerMock;
    private readonly Mock<IGraphicalDebugger> _graphicalDebuggerMock;
    private readonly Mock<IMapImageFactory> _mapImageFactoryMock;
    private readonly Mock<IMapFileNameFormatter> _mapFileNameFormatterMock;

    private readonly RayCastingChokeFinder _chokeFinder;

    private const int MapSize = 20;

    public RayCastingChokeFinderTests()
    {
        _loggerMock = new Mock<ILogger>();
        _terrainTrackerMock = new Mock<ITerrainTracker>();
        _graphicalDebuggerMock = new Mock<IGraphicalDebugger>();
        _mapImageFactoryMock = new Mock<IMapImageFactory>();
        _mapFileNameFormatterMock = new Mock<IMapFileNameFormatter>();

        var walkableCells = new HashSet<Vector2>();
        for (var x = 0; x < MapSize; x++)
        {
            for (var y = 0; y < MapSize; y++)
            {
                walkableCells.Add(new Vector2(x, y));
            }
        }

        _terrainTrackerMock.Setup(t => t.Cells).Returns(walkableCells);
        _terrainTrackerMock.Setup(t => t.MaxX).Returns(MapSize);
        _terrainTrackerMock.Setup(t => t.MaxY).Returns(MapSize);

        _chokeFinder = new RayCastingChokeFinder(
            _loggerMock.Object,
            _terrainTrackerMock.Object,
            _graphicalDebuggerMock.Object,
            _mapImageFactoryMock.Object,
            _mapFileNameFormatterMock.Object,
            "test_map"
        );
    }

    private List<VisionLine> CreateLinesAtAnAngle(int angle)
    {
        var method = typeof(RayCastingChokeFinder).GetMethod("CreateLinesAtAnAngle", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var result = method.Invoke(_chokeFinder, new object[] { angle, MapSize, MapSize });
        Assert.NotNull(result);

        return (List<VisionLine>)result;
    }

    private List<VisionLine> BreakDownIntoContinuousSegments(List<VisionLine> lines)
    {
        var method = typeof(RayCastingChokeFinder).GetMethod("BreakDownIntoContinuousSegments", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(IEnumerable<VisionLine>) }, null);
        Assert.NotNull(method);

        var result = method.Invoke(_chokeFinder, new object[] { lines });
        Assert.NotNull(result);

        return (List<VisionLine>)result;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(45)]
    [InlineData(90)]
    [InlineData(135)]
    public void CreateLinesAtAnAngle_WithAngle_ShouldCoverAllCells(int angle)
    {
        // Arrange
        var walkableCellsCount = _terrainTrackerMock.Object.Cells.Count;

        // Act
        var rawLines = CreateLinesAtAnAngle(angle);
        var lines = BreakDownIntoContinuousSegments(rawLines);
        var coveredCellsCount = lines.SelectMany(line => line.OrderedTraversedCells).ToHashSet().Count;

        // Assert
        Assert.Equal(walkableCellsCount, coveredCellsCount);
    }
}
