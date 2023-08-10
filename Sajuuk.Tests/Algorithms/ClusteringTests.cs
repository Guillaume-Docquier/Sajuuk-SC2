using System.Numerics;
using Moq;
using Sajuuk.Algorithms;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameSense;
using Sajuuk.Tests.Fixtures;

namespace Sajuuk.Tests.Algorithms;

public class ClusteringTests : IClassFixture<NoLoggerFixture> {
    private readonly Mock<ITerrainTracker> _terrainTrackerMock = new Mock<ITerrainTracker>();

    [Fact]
    public void GivenOneGroupOfCells_WhenFloodFill_ThenReturnsAllTheCells() {
        // Arrange
        //  --XX
        //  X-X-
        //  XXX-
        var cells = new HashSet<Vector2>
        {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 0),
            new Vector2(2, 0),
            new Vector2(2, 1),
            new Vector2(2, 2),
            new Vector2(3, 2),
        };

        _terrainTrackerMock
            .Setup(terrainTracker => terrainTracker.GetReachableNeighbors(It.IsAny<Vector2>(), It.IsAny<HashSet<Vector2>>(), It.IsAny<bool>()))
            .Returns<Vector2, IReadOnlySet<Vector2>, bool>((position, potentialNeighbors, _) => {
                var neighbors = new List<Vector2>
                {
                    position.Translate(xTranslation: 1),
                    position.Translate(xTranslation: -1),
                    position.Translate(yTranslation: 1),
                    position.Translate(yTranslation: -1),
                };

                return neighbors
                    .Where(potentialNeighbors.Contains)
                    .ToHashSet();
            });

        var clustering = new Clustering(_terrainTrackerMock.Object, Mock.Of<IGraphicalDebugger>());

        // Act
        var floodFill = clustering.FloodFill(cells, cells.First());

        // Assert
        var expected = cells.OrderBy(cell => cell.X).ThenBy(cell => cell.Y).ToList();
        var actual = floodFill.OrderBy(cell => cell.X).ThenBy(cell => cell.Y).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GivenMultipleGroupsOfCells_WhenFloodFill_ThenReturnsTheGroupThatContainsTheStartingCell() {
        // Arrange
        //  XX-XX-XX
        //  XX-XX-XX
        var group1 = new List<Vector2>
        {
            new Vector2(0, 0),
            new Vector2(0, 1),
            new Vector2(1, 0),
            new Vector2(1, 1),
        };

        var group2 = new List<Vector2>
        {
            new Vector2(3, 3),
            new Vector2(3, 4),
            new Vector2(4, 3),
            new Vector2(4, 4),
        };

        var group3 = new List<Vector2>
        {
            new Vector2(6, 6),
            new Vector2(6, 7),
            new Vector2(7, 6),
            new Vector2(7, 7),
        };

        _terrainTrackerMock
            .Setup(terrainTracker => terrainTracker.GetReachableNeighbors(It.IsAny<Vector2>(), It.IsAny<HashSet<Vector2>>(), It.IsAny<bool>()))
            .Returns<Vector2, IReadOnlySet<Vector2>, bool>((position, potentialNeighbors, _) => {
                var neighbors = new List<Vector2>
                {
                    position.Translate(xTranslation: 1),
                    position.Translate(xTranslation: -1),
                    position.Translate(yTranslation: 1),
                    position.Translate(yTranslation: -1),
                };

                return neighbors
                    .Where(potentialNeighbors.Contains)
                    .ToHashSet();
            });

        var clustering = new Clustering(_terrainTrackerMock.Object, Mock.Of<IGraphicalDebugger>());

        // Act
        var floodFill = clustering.FloodFill(group1.Concat(group2).Concat(group3).ToHashSet(), group1.First());

        // Assert
        var expected = group1.OrderBy(cell => cell.X).ThenBy(cell => cell.Y).ToList();
        var actual = floodFill.OrderBy(cell => cell.X).ThenBy(cell => cell.Y).ToList();
        Assert.Equal(expected, actual);
    }
}
