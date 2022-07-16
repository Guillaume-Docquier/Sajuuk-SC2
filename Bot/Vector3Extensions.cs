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

    public static Vector3 DirectionTo(this Vector3 origin, Vector3 destination, bool ignoreZAxis = true) {
        var direction = Vector3.Normalize(destination - origin);
        if (ignoreZAxis) {
            direction.Z = 0;
        }

        return direction;
    }

    public static Vector3 TranslateTowards(this Vector3 origin, Vector3 destination, float distance, bool ignoreZAxis = true) {
        var direction = origin.DirectionTo(destination, ignoreZAxis);

        return origin + direction * distance;
    }

    public static Vector3 TranslateAwayFrom(this Vector3 origin, Vector3 destination, float distance, bool ignoreZAxis = true) {
        var direction = origin.DirectionTo(destination, ignoreZAxis);

        return origin - direction * distance;
    }
}
