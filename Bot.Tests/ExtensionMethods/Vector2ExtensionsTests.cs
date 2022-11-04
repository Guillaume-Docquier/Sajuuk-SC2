using System.Numerics;
using Bot.ExtensionMethods;
using Bot.Utils;

namespace Bot.Tests.ExtensionMethods;

public class Vector2ExtensionsTests {
    private const float RotationPrecision = 0.000002f;

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

        Assert.InRange(expected.X, actual.X - RotationPrecision, actual.X + RotationPrecision);
        Assert.InRange(expected.Y, actual.Y - RotationPrecision, actual.Y + RotationPrecision);
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

        Assert.InRange(expected.X, actual.X - RotationPrecision, actual.X + RotationPrecision);
        Assert.InRange(expected.Y, actual.Y - RotationPrecision, actual.Y + RotationPrecision);
    }
}
