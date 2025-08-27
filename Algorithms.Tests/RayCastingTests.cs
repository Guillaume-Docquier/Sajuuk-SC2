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

    private enum Direction {
        Up,
        Right,
        Down,
        Left
    }

    public static IEnumerable<object[]> PointsThatTravelInDiagonal() {
        var startingPoint = new Vector2(0.5f, 0.5f);
        var directionOffsets = new Dictionary<Direction, Vector2> {
            { Direction.Up,    new Vector2(0, 0.4f) },
            { Direction.Right, new Vector2(0.4f, 0) },
            { Direction.Down,  new Vector2(0, -0.4f) },
            { Direction.Left,  new Vector2(-0.4f, 0) },
        };
        var directionSteps = new Dictionary<Direction, Vector2> {
            { Direction.Up,    new Vector2(0, 1) },
            { Direction.Right, new Vector2(1, 0) },
            { Direction.Down,  new Vector2(0, -1) },
            { Direction.Left,  new Vector2(-1, 0) },
        };

        // TODO GD Document all of this
        object[] Generate(Direction mainDirection, Direction secondaryDirection, int steps) {
            var from = startingPoint + directionOffsets[mainDirection];

            var expectedPath = new List<Vector2> { Vector2.Zero };
            var currentPoint = Vector2.Zero;

            for (var i = 1; i <= steps; i++) {
                var useMainDirection = i % 2 != 0;
                var directionVector = useMainDirection
                    ? directionSteps[mainDirection]
                    : directionSteps[secondaryDirection];

                currentPoint += directionVector;
                expectedPath.Add(currentPoint);
            }

            var towards = expectedPath.Last() + startingPoint + (steps % 2 == 0 ? directionOffsets[mainDirection] : directionOffsets[secondaryDirection]);

            return new object[]
            {
                from,
                towards,
                expectedPath,
            };
        };

        for (int steps = 1; steps <= 3; steps++) {
            yield return Generate(Direction.Up, Direction.Left, steps + 10000);
            yield return Generate(Direction.Up, Direction.Right, steps + 10000);
            yield return Generate(Direction.Right, Direction.Up, steps + 10000);
            yield return Generate(Direction.Right, Direction.Down, steps + 10000);
            yield return Generate(Direction.Down, Direction.Right, steps + 10000);
            yield return Generate(Direction.Down, Direction.Left, steps + 10000);
            yield return Generate(Direction.Left, Direction.Down, steps + 10000);
            yield return Generate(Direction.Left, Direction.Up, steps + 10000);
        }

        yield return new object[] { new Vector2(0, 0), new Vector2(0, 0), new List<Vector2> { new(0, 0)}, };
    }

    [Theory]
    [MemberData(nameof(PointsThatTravelInDiagonal))]
    public void RayCastPosToPos_ShouldRayCastCorrectlyEvenAtLongRanges(Vector2 origin, Vector2 towards, List<Vector2> expectedPath) {
        // Act
        var actualPath = RayCasting.RayCastPosToPos(origin, towards).Select(rayCastResult => rayCastResult.Cell).ToList();

        // Assert
        Assert.Equal(expectedPath, actualPath);
    }
}
