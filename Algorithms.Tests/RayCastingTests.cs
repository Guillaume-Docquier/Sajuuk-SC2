using System.Numerics;
using Algorithms.ExtensionMethods;
using FluentAssertions;

namespace Algorithms.Tests;

public class RayCastingTests {
    public static IEnumerable<object[]> PairOfDifferentCellsAroundTheOrigin() {
        var origin = Vector2.Zero;

        for (var x = -25; x < 25; x++) {
            for (var y = -25; y < 25; y++) {
                if (x == 0 && y == 0) {
                    continue;
                }

                yield return new object[] { origin, new Vector2(x, y) };
            }
        }

        // Found to infinite raycast
        yield return new object[] { new Vector2(163f, -81.86624f), new Vector2(-84.16836f, 38.68582f) }; // Going up left, hits (-83.5 39.5)
    }

    [Theory]
    [MemberData(nameof(PairOfDifferentCellsAroundTheOrigin))]
    public void RayCastCellToCell_ShouldReturnForAnyTwoDifferentCells(Vector2 origin, Vector2 destination) {
        // Act
        var result = RayCasting.RayCastCellToCell(origin, destination).ToList();

        // Assert
        result.Count.Should().BeGreaterThan(1);
    }

    [Theory]
    [MemberData(nameof(PairOfDifferentCellsAroundTheOrigin))]
    public void RayCastPosToPos_ShouldReturnForAnyTwoDifferentCells(Vector2 origin, Vector2 destination) {
        // Act
        var result = RayCasting.RayCastPosToPos(origin, destination).ToList();

        // Assert
        result.Count.Should().BeGreaterThan(1);
    }

    public static IEnumerable<object[]> PointsWithinTheSameCell() {
        for (var x = 1f; x < 2; x += 0.1f) {
            for (var y = 1f; y < 2; y += 0.1f) {
                yield return new object[] { new Vector2(x, y) };
            }
        }
    }

    [Theory]
    [MemberData(nameof(PointsWithinTheSameCell))]
    public void RayCastCellToCell_ShouldNotCareAboutTheExactCoordinates(Vector2 destination) {
        // Arrange
        var origin = Vector2.Zero;

        // Act
        var result = RayCasting.RayCastCellToCell(origin, destination).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Cell.Should().BeEquivalentTo(origin.AsCell());
        result[1].Cell.Should().BeEquivalentTo(destination.AsCell());
    }

    public static IEnumerable<object[]> PointsThatTravelInDiagonal() {
        yield return new object[] { new Vector2(0, 0), new Vector2(0, 0), new Vector2[] { new(0, 0)}, };

        // Left, down/up
        yield return new object[] { new Vector2(0.1f, 0.5f), new Vector2(-0.5f, -0.1f), new Vector2[] { new(0, 0), new(-1, 0), new(-1, -1) }, };
        yield return new object[] { new Vector2(0.1f, 0.5f), new Vector2(-0.5f, 0.1f), new Vector2[] { new(0, 0), new(-1, 0) }, };
        yield return new object[] { new Vector2(0.1f, 0.5f), new Vector2(-0.5f, 1.1f), new Vector2[] { new(0, 0), new(-1, 0), new(-1, 1) }, };
        yield return new object[] { new Vector2(0.1f, 0.5f), new Vector2(-0.5f, 0.9f), new Vector2[] { new(0, 0), new(-1, 0) }, };

        // Bottom, left/right
        yield return new object[] { new Vector2(0.5f, 0.1f), new Vector2(-0.1f, -0.5f), new Vector2[] { new(0, 0), new(0, -1), new(-1, -1) }, };
        yield return new object[] { new Vector2(0.5f, 0.1f), new Vector2(0.1f, -0.5f), new Vector2[] { new(0, 0), new(0, -1) }, };
        yield return new object[] { new Vector2(0.5f, 0.1f), new Vector2(1.1f, -0.5f), new Vector2[] { new(0, 0), new(0, -1), new(1, -1) }, };
        yield return new object[] { new Vector2(0.5f, 0.1f), new Vector2(0.9f, -0.5f), new Vector2[] { new(0, 0), new(0, -1) }, };

        // Up, left/right
        yield return new object[] { new Vector2(0.5f, 0.9f), new Vector2(-0.1f, 1.5f), new Vector2[] { new(0, 0), new(0, 1), new(-1, 1) }, };
        yield return new object[] { new Vector2(0.5f, 0.9f), new Vector2(0.1f, 1.5f), new Vector2[] { new(0, 0), new(0, 1) }, };
        yield return new object[] { new Vector2(0.5f, 0.9f), new Vector2(1.1f, 1.5f), new Vector2[] { new(0, 0), new(0, 1), new(1, 1) }, };
        yield return new object[] { new Vector2(0.5f, 0.9f), new Vector2(0.9f, 1.5f), new Vector2[] { new(0, 0), new(0, 1) }, };

        // Right down/up
        yield return new object[] { new Vector2(0.9f, 0.5f), new Vector2(1.5f, -0.1f), new Vector2[] { new(0, 0), new(1, 0), new(1, -1) }, };
        yield return new object[] { new Vector2(0.9f, 0.5f), new Vector2(1.5f, 0.1f), new Vector2[] { new(0, 0), new(1, 0) }, };
        yield return new object[] { new Vector2(0.9f, 0.5f), new Vector2(1.5f, 1.1f), new Vector2[] { new(0, 0), new(1, 0), new(1, 1) }, };
        yield return new object[] { new Vector2(0.9f, 0.5f), new Vector2(1.5f, 0.9f), new Vector2[] { new(0, 0), new(1, 0) }, };
    }

    [Theory]
    [MemberData(nameof(PointsThatTravelInDiagonal))]
    public void RayCastPosToPos_ShouldRayCastCorrectlyInSimpleCases(Vector2 origin, Vector2 towards, Vector2[] expectedPath) {
        // Act
        var path = RayCasting.RayCastPosToPos(origin, towards);

        // Assert
        Assert.Equal(expectedPath, path.Select(rayCastResult => rayCastResult.Cell));
    }
}
