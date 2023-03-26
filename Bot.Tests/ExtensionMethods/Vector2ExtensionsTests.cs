using System.Numerics;
using Bot.ExtensionMethods;
using Bot.Utils;

namespace Bot.Tests.ExtensionMethods;

public class Vector2ExtensionsTests {
    private const float RotateAroundPrecision = 0.000002f;
    private const float AngleToPrecision = 0.00002f;

    public static IEnumerable<object[]> AnglesIn360() {
        for (var angle = 0; angle < 360; angle++) {
            yield return new object[] { angle };
        }
    }

    [Theory]
    [MemberData(nameof(AnglesIn360))]
    public void GivenAPositionAboveTheOriginAndAPositiveAngle_WhenRotatingAroundTheOrigin_RotatesCounterClockwiseWith6DigitsPrecision(float angle) {
        // Arrange
        var pointToRotate = new Vector2(0, 1);

        // Act
        var actual = pointToRotate.RotateAround(new Vector2(), MathUtils.DegToRad(angle));

        // Assert
        var rotationMatrix = Matrix4x4.CreateRotationZ((float)MathUtils.DegToRad(angle));
        var expected = Vector2.Transform(pointToRotate, rotationMatrix);

        Assert.InRange(actual.X, expected.X - RotateAroundPrecision, expected.X + RotateAroundPrecision);
        Assert.InRange(actual.Y, expected.Y - RotateAroundPrecision, expected.Y + RotateAroundPrecision);
    }

    [Theory]
    [MemberData(nameof(AnglesIn360))]
    public void GivenAPositionAboveTheOriginAndAPositiveAngle_WhenRotatingAroundAPoint_RotatesCounterClockwiseAroundThePointWith6DigitsPrecision(float angle) {
        // Arrange
        var pointToRotate = new Vector2(0, 1);
        var origin = new Vector2(2, 5);

        // Act
        var actual = pointToRotate.RotateAround(origin, MathUtils.DegToRad(angle));

        // Assert
        var displaceOriginMatrix = Matrix4x4.CreateTranslation(origin.ToVector3() * -1);
        var rotationMatrix = Matrix4x4.CreateRotationZ((float)MathUtils.DegToRad(angle));
        var restoreOriginMatrix = Matrix4x4.CreateTranslation(origin.ToVector3());

        var expected = Vector2.Transform(pointToRotate, displaceOriginMatrix);
        expected = Vector2.Transform(expected, rotationMatrix);
        expected = Vector2.Transform(expected, restoreOriginMatrix);

        Assert.InRange(actual.X, expected.X - RotateAroundPrecision, expected.X + RotateAroundPrecision);
        Assert.InRange(actual.Y, expected.Y - RotateAroundPrecision, expected.Y + RotateAroundPrecision);
    }

    public static IEnumerable<object[]> Vector2AnglesOrigin() {
        var origin = new Vector2(0, 0);
        var pointToRotate = new Vector2(1, 0);

        for (var angle = -360; angle <= 360; angle++) {
            var radAngle = MathUtils.DegToRad(angle);
            var expectedRadAngle = MathUtils.DegToRad(
                angle <= -180 ? angle + 360
                : angle > 180 ? angle - 360
                : angle
            );

            yield return new object[] { origin, pointToRotate.RotateAround(origin, radAngle), expectedRadAngle };
        }
    }

    [Theory]
    [MemberData(nameof(Vector2AnglesOrigin))]
    public void GivenTheOriginAndAPosition_WhenGetRadAngleTo_ThenReturnsAngleInRadiansBetweenPiIncludedAndMinusPiExcluded(Vector2 origin, Vector2 destination, double expectedRadAngle) {
        // Act
        var actualAngle = origin.GetRadAngleTo(destination);

        // Assert
        Assert.InRange(actualAngle, expectedRadAngle - AngleToPrecision, expectedRadAngle + AngleToPrecision);
    }

    public static IEnumerable<object[]> Vector2Angles() {
        var origin = new Vector2(0, 0);
        var pointToRotate = new Vector2(1, 0);

        for (var angle = -360; angle <= 360; angle++) {
            var radAngle = MathUtils.DegToRad(angle);
            var expectedRadAngle = MathUtils.DegToRad(
                angle <= -180 ? angle + 360
                : angle > 180 ? angle - 360
                : angle
            );

            var offset = new Vector2(angle, angle);
            var offsetOrigin = origin + offset;
            var offsetPointToRotate = pointToRotate + offset;

            yield return new object[] { offsetOrigin, offsetPointToRotate.RotateAround(offsetOrigin, radAngle), expectedRadAngle };
        }
    }

    [Theory]
    [MemberData(nameof(Vector2Angles))]
    public void GivenTwoPositions_WhenGetRadAngleTo_ThenReturnsAngleInRadiansBetweenPiIncludedAndMinusPiExcluded(Vector2 origin, Vector2 destination, double expectedRadAngle) {
        // Act
        var actualAngle = origin.GetRadAngleTo(destination);

        // Assert
        Assert.InRange(actualAngle, expectedRadAngle - AngleToPrecision, expectedRadAngle + AngleToPrecision);
    }
}
