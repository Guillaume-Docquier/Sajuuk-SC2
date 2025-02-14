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
    }

    [Theory]
    [MemberData(nameof(PairOfDifferentCellsAroundTheOrigin))]
    public void RayCastCellToCell_ShouldReturnForAnyTwoDifferentCells(Vector2 origin, Vector2 destination) {
        // Act
        var result = RayCasting.RayCastCellToCell(origin, destination).ToList();

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
}
