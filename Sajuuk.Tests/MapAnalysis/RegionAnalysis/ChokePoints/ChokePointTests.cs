using System.Numerics;
using Moq;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis.RegionAnalysis.ChokePoints;
using Sajuuk.Tests.Fixtures;

namespace Sajuuk.Tests.MapAnalysis.RegionAnalysis.ChokePoints;

public class ChokePointTests : IClassFixture<NoLoggerFixture> {
    private readonly Mock<ITerrainTracker> _terrainTrackerMock = new Mock<ITerrainTracker>();

    [Fact]
    public void GivenLineWithoutDiagonals_WhenCallingConstructor_EdgeIsTheOriginalLine() {
        // Arrange
        var start = new Vector2(0.5f, 0.5f);
        var end = new Vector2(0.5f, 3.5f);

        _terrainTrackerMock
            .Setup(terrainTracker => terrainTracker.IsWalkable(It.IsAny<Vector2>(), It.IsAny<bool>()))
            .Returns(true);

        // Act
        var chokePoint = new ChokePoint(start, end, _terrainTrackerMock.Object);

        // Assert
        var expectedEdge = new List<Vector2>
        {
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 1.5f),
            new Vector2(0.5f, 2.5f),
            new Vector2(0.5f, 3.5f)
        };

        Assert.Equal(expectedEdge, chokePoint.Edge);
    }

    [Fact]
    public void GivenLineWithDiagonals_WhenCallingConstructor_AddsCellsToEdgeToEliminateDiagonals() {
        // Arrange
        var start = new Vector2(0.5f, 0.5f);
        var end = new Vector2(3.5f, 3.5f);

        _terrainTrackerMock
            .Setup(terrainTracker => terrainTracker.IsWalkable(It.IsAny<Vector2>(), It.IsAny<bool>()))
            .Returns(true);

        // Act
        var chokePoint = new ChokePoint(start, end, _terrainTrackerMock.Object);

        // Assert
        var expectedEdge = new List<Vector2>
        {
            new Vector2(0.5f, 0.5f),
            new Vector2(1.5f, 0.5f),
            new Vector2(1.5f, 1.5f),
            new Vector2(2.5f, 1.5f),
            new Vector2(2.5f, 2.5f),
            new Vector2(3.5f, 2.5f),
            new Vector2(3.5f, 3.5f)
        };

        Assert.Equal(expectedEdge, chokePoint.Edge);
    }

    [Fact]
    public void GivenWalkableStart_WhenCallingConstructor_StartIsStart() {
        // Arrange
        var start = new Vector2(0.5f, 0.5f);
        var end = new Vector2(3.5f, 3.5f);

        _terrainTrackerMock
            .Setup(terrainTracker => terrainTracker.IsWalkable(It.IsAny<Vector2>(), It.IsAny<bool>()))
            .Returns(true);

        // Act
        var chokePoint = new ChokePoint(start, end, _terrainTrackerMock.Object);

        // Assert
        Assert.Equal(start, chokePoint.Start);
    }

    [Fact]
    public void GivenUnwalkableStart_WhenCallingConstructor_StartIsNextWalkableCell() {
        // Arrange
        var start = new Vector2(0.5f, 0.5f);
        var end = new Vector2(3.5f, 3.5f);

        _terrainTrackerMock
            .Setup(terrainTracker => terrainTracker.IsWalkable(It.IsAny<Vector2>(), It.IsAny<bool>()))
            .Returns(true);

        _terrainTrackerMock
            .Setup(terrainTracker => terrainTracker.IsWalkable(start, It.IsAny<bool>()))
            .Returns(false);

        // Act
        var chokePoint = new ChokePoint(start, end, _terrainTrackerMock.Object);

        // Assert
        var expectedStart = new Vector2(1.5f, 0.5f); // Corrected diagonal
        Assert.Equal(expectedStart, chokePoint.Start);
    }

    [Fact]
    public void GivenWalkableEnd_WhenCallingConstructor_EndIsEnd() {
        // Arrange
        var start = new Vector2(0.5f, 0.5f);
        var end = new Vector2(3.5f, 3.5f);

        _terrainTrackerMock
            .Setup(terrainTracker => terrainTracker.IsWalkable(It.IsAny<Vector2>(), It.IsAny<bool>()))
            .Returns(true);

        // Act
        var chokePoint = new ChokePoint(start, end, _terrainTrackerMock.Object);

        // Assert
        Assert.Equal(end, chokePoint.End);
    }

    [Fact]
    public void GivenUnwalkableEnd_WhenCallingConstructor_EndIsPreviousWalkableCell() {
        // Arrange
        var start = new Vector2(0.5f, 0.5f);
        var end = new Vector2(3.5f, 3.5f);

        _terrainTrackerMock
            .Setup(terrainTracker => terrainTracker.IsWalkable(It.IsAny<Vector2>(), It.IsAny<bool>()))
            .Returns(true);

        _terrainTrackerMock
            .Setup(terrainTracker => terrainTracker.IsWalkable(end, It.IsAny<bool>()))
            .Returns(false);

        // Act
        var chokePoint = new ChokePoint(start, end, _terrainTrackerMock.Object);

        // Assert
        var expectedEnd = new Vector2(3.5f, 2.5f); // Corrected diagonal
        Assert.Equal(expectedEnd, chokePoint.End);
    }
}
