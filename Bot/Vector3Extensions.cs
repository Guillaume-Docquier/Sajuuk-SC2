using System.Numerics;
using SC2APIProtocol;

namespace Bot;

public static class Vector3Extensions {
    public static Point ToPoint(this Vector3 vector, float xOffset = 0, float yOffset = 0, float zOffset = 0) {
        return new Point
        {
            X = vector.X + xOffset,
            Y = vector.Y + yOffset,
            Z = vector.Z + zOffset,
        };
    }

    public static Point2D ToPoint2D(this Vector3 vector) {
        return new Point2D { X = vector.X, Y = vector.Y };
    }
}
