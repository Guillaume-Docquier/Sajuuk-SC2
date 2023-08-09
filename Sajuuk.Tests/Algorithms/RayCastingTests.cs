using System.Numerics;
using Sajuuk.Algorithms;
using Sajuuk.ExtensionMethods;
using Sajuuk.Utils;

namespace Sajuuk.Tests.Algorithms;

public class RayCastingTests {
    public static IEnumerable<object[]> StraightLines() {
        yield return new object[] { new Vector2(0, 0).AsWorldGridCenter(), new Vector2( 0,  5).AsWorldGridCenter(), 6 };
        yield return new object[] { new Vector2(0, 0).AsWorldGridCenter(), new Vector2( 0, -5).AsWorldGridCenter(), 6 };
        yield return new object[] { new Vector2(0, 0).AsWorldGridCenter(), new Vector2( 5,  0).AsWorldGridCenter(), 6 };
        yield return new object[] { new Vector2(0, 0).AsWorldGridCenter(), new Vector2(-5,  0).AsWorldGridCenter(), 6 };
    }

    [Theory]
    [MemberData(nameof(StraightLines))]
    public void GivenRaysWithoutSlopes_WhenRayCasting_CastsUntilTargetIsMet(Vector2 origin, Vector2 target, int expectedNumberOfCrossedCells) {
        // Arrange

        // Act
        var rayCastingResults = RayCasting.RayCast(origin, target, cell => cell.AsWorldGridCenter() == target).ToList();

        // Assert
        Assert.Equal(expectedNumberOfCrossedCells, rayCastingResults.Count);
        for (var i = 0; i < rayCastingResults.Count; i++) {
            var expectedCell = Vector2.Lerp(origin, target, (float)i / (rayCastingResults.Count - 1)).AsWorldGridCorner();
            var expectedDistance = 0f;
            if (i > 0) {
                expectedDistance = i * 1f - 1f / 2;
            }

            Assert.Equal(expectedCell, rayCastingResults[i].CornerOfCell);
            Assert.Equal(expectedDistance, origin.DistanceTo(rayCastingResults[i].RayIntersection));
        }
    }

    public static IEnumerable<object[]> DiagonalLines() {
        yield return new object[] { new Vector2(0, 0).AsWorldGridCenter(), new Vector2( 5,  5).AsWorldGridCenter(), 6 };
        yield return new object[] { new Vector2(0, 0).AsWorldGridCenter(), new Vector2( 5, -5).AsWorldGridCenter(), 6 };
        yield return new object[] { new Vector2(0, 0).AsWorldGridCenter(), new Vector2( 5, -5).AsWorldGridCenter(), 6 };
        yield return new object[] { new Vector2(0, 0).AsWorldGridCenter(), new Vector2(-5, -5).AsWorldGridCenter(), 6 };
    }

    [Theory]
    [MemberData(nameof(DiagonalLines))]
    public void GivenDiagonalRays_WhenRayCasting_CastsUntilTargetIsMet(Vector2 origin, Vector2 target, int expectedNumberOfCrossedCells) {
        // Arrange

        // Act
        var rayCastingResults = RayCasting.RayCast(origin, target, cell => cell.AsWorldGridCenter() == target).ToList();

        // Assert
        Assert.Equal(expectedNumberOfCrossedCells, rayCastingResults.Count);
        var diagonalDistance = Math.Sqrt(2);
        for (var i = 0; i < rayCastingResults.Count; i++) {
            var expectedCell = Vector2.Lerp(origin, target, (float)i / (rayCastingResults.Count - 1)).AsWorldGridCorner();
            var expectedDistance = 0f;
            if (i > 0) {
                expectedDistance = (float)(i * diagonalDistance - diagonalDistance / 2);
            }

            Assert.Equal(expectedCell, rayCastingResults[i].CornerOfCell);
            Assert.Equal(expectedDistance, origin.DistanceTo(rayCastingResults[i].RayIntersection));
        }
    }

    public static IEnumerable<object[]> Angles() {
        yield return new object[]
        {
            new Vector2(0, 0).AsWorldGridCenter(),
            new Vector2(0, 2).AsWorldGridCenter(),
            0,
            new List<Vector2> { new Vector2(0, 0).AsWorldGridCenter(), new Vector2(0, 1).AsWorldGridCenter(), new Vector2(0, 2).AsWorldGridCenter() }
        };
        yield return new object[] {
            new Vector2(0, 0).AsWorldGridCenter(),
            new Vector2(-2, 2).AsWorldGridCenter(),
            45,
            // Probably due to rounding errors, 45 degrees doesn't create a clean line (but 135 and 315 do)
            new List<Vector2>
            {
                new Vector2(0, 0).AsWorldGridCenter(),
                new Vector2(0, 1).AsWorldGridCenter(),
                new Vector2(-1, 1).AsWorldGridCenter(),
                new Vector2(-1, 2).AsWorldGridCenter(),
                new Vector2(-2, 2).AsWorldGridCenter()
            }
        };
        yield return new object[] {
            new Vector2(0, 0).AsWorldGridCenter(),
            new Vector2(-2, 0).AsWorldGridCenter(),
            90,
            new List<Vector2> { new Vector2(0, 0).AsWorldGridCenter(), new Vector2(-1, 0).AsWorldGridCenter(), new Vector2(-2, 0).AsWorldGridCenter() }
        };
        yield return new object[] {
            new Vector2(0, 0).AsWorldGridCenter(),
            new Vector2(-2, -2).AsWorldGridCenter(),
            135,
            new List<Vector2> { new Vector2(0, 0).AsWorldGridCenter(), new Vector2(-1, -1).AsWorldGridCenter(), new Vector2(-2, -2).AsWorldGridCenter() }
        };
        yield return new object[] {
            new Vector2(0, 0).AsWorldGridCenter(),
            new Vector2(0, -2).AsWorldGridCenter(),
            180,
            new List<Vector2> { new Vector2(0, 0).AsWorldGridCenter(), new Vector2(0, -1).AsWorldGridCenter(), new Vector2(0, -2).AsWorldGridCenter() }
        };
        yield return new object[] {
            new Vector2(0, 0).AsWorldGridCenter(),
            new Vector2(2, -2).AsWorldGridCenter(),
            225,
            // Probably due to rounding errors, 225 degrees doesn't create a clean line (but 135 and 315 do)
            new List<Vector2>
            {
                new Vector2(0, 0).AsWorldGridCenter(),
                new Vector2(1, 0).AsWorldGridCenter(),
                new Vector2(1, -1).AsWorldGridCenter(),
                new Vector2(2, -1).AsWorldGridCenter(),
                new Vector2(2, -2).AsWorldGridCenter()
            }
        };
        yield return new object[] {
            new Vector2(0, 0).AsWorldGridCenter(),
            new Vector2(2, 0).AsWorldGridCenter(),
            270,
            new List<Vector2> { new Vector2(0, 0).AsWorldGridCenter(), new Vector2(1, 0).AsWorldGridCenter(), new Vector2(2, 0).AsWorldGridCenter() }
        };
        yield return new object[] {
            new Vector2(0, 0).AsWorldGridCenter(),
            new Vector2(2, 2).AsWorldGridCenter(),
            315,
            new List<Vector2> { new Vector2(0, 0).AsWorldGridCenter(), new Vector2(1, 1).AsWorldGridCenter(), new Vector2(2, 2).AsWorldGridCenter() }
        };
        yield return new object[] {
            new Vector2(0, 0).AsWorldGridCenter(),
            new Vector2(0, 2).AsWorldGridCenter(),
            360,
            new List<Vector2> { new Vector2(0, 0).AsWorldGridCenter(), new Vector2(0, 1).AsWorldGridCenter(), new Vector2(0, 2).AsWorldGridCenter() }
        };
    }

    [Theory]
    [MemberData(nameof(Angles))]
    public void GivenAngles_WhenRayCasting_CastsUntilTargetIsMet(Vector2 origin, Vector2 target, double degAngle, List<Vector2> expectedCrossedCells) {
        // Arrange
        var radAngle = MathUtils.DegToRad(degAngle);

        // Act
        var rayCastingResults = RayCasting
            .RayCast(origin, radAngle, cell => cell.AsWorldGridCenter() == target || cell.AsWorldGridCenter().DistanceTo(origin) > target.DistanceTo(origin))
            .Select(rayCastingResult => rayCastingResult.CornerOfCell.AsWorldGridCenter())
            .ToList();

        // Assert
        Assert.Equal(expectedCrossedCells, rayCastingResults);
    }
}
