using System.Numerics;
using Bot.Algorithms;
using Bot.ExtensionMethods;

namespace Bot.Tests.Algorithms;

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
}
