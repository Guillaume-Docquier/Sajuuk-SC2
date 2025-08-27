using System.Numerics;
using Algorithms.ExtensionMethods;
using SC2APIProtocol;
using SC2Client.State;

namespace SC2Client.ExtensionMethods;

public static class Vector2Extensions {
    public static Point2D ToPoint2D(this Vector2 vector, float xOffset = 0, float yOffset = 0) {
        return new Point2D
        {
            X = vector.X + xOffset,
            Y = vector.Y + yOffset,
        };
    }

    public static float DistanceTo(this Vector2 origin, IUnit unit) {
        return Vector2.Distance(origin, unit.Position.ToVector2());
    }
}
