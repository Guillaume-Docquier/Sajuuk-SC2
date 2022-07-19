using System;
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

    public static Vector3 Translate(this Vector3 origin, float xTranslation = 0, float yTranslation = 0, float zTranslation = 0) {
        return new Vector3 { X = origin.X + xTranslation, Y = origin.Y + yTranslation, Z = origin.Z + zTranslation };
    }

    public static float HorizontalDistance(this Vector3 origin, Vector3 destination) {
        var deltaX = origin.X - destination.X;
        var deltaY = origin.Y - destination.Y;

        return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }
}
