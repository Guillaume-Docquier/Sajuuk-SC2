﻿using System.Numerics;

namespace Algorithms.ExtensionMethods;

public static class Vector3Extensions {
    public static Vector2 ToVector2(this Vector3 vector) {
        return new Vector2(vector.X, vector.Y);
    }

    public static float DistanceTo(this Vector3 origin, Vector3 destination) {
        return Vector3.Distance(origin, destination);
    }

    // TODO GD I don't know why I added ignoreZAxis, but it's whack. If you don't care about Z, use Vector2!
    public static Vector3 DirectionTo(this Vector3 origin, Vector3 destination, bool ignoreZAxis = true) {
        var direction = Vector3.Normalize(destination - origin);
        if (ignoreZAxis) {
            direction.Z = 0;
        }

        return direction;
    }

    // TODO GD I don't know why I added ignoreZAxis, but it's whack. If you don't care about Z, use Vector2!
    public static Vector3 TranslateTowards(this Vector3 origin, Vector3 destination, float distance, bool ignoreZAxis = true) {
        var direction = origin.DirectionTo(destination, ignoreZAxis);

        return origin + direction * distance;
    }

    // TODO GD I don't know why I added ignoreZAxis, but it's whack. If you don't care about Z, use Vector2!
    public static Vector3 TranslateAwayFrom(this Vector3 origin, Vector3 destination, float distance, bool ignoreZAxis = true) {
        var direction = origin.DirectionTo(destination, ignoreZAxis);

        return origin - direction * distance;
    }

    public static Vector3 Translate(this Vector3 origin, float xTranslation = 0, float yTranslation = 0, float zTranslation = 0) {
        return new Vector3 { X = origin.X + xTranslation, Y = origin.Y + yTranslation, Z = origin.Z + zTranslation };
    }

    public static float HorizontalDistanceTo(this Vector3 origin, Vector3 destination) {
        var deltaX = origin.X - destination.X;
        var deltaY = origin.Y - destination.Y;

        return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    /// <summary>
    /// Rotates the given position by a certain angle in radians with respect to a given origin, or (0, 0, 0)
    /// </summary>
    /// <param name="position">The position to rotate</param>
    /// <param name="angleInRadians">The angle in radians to rotate by</param>
    /// <param name="origin">The origin to rotate around</param>
    /// <returns>The resulting position</returns>
    public static Vector3 Rotate2D(this Vector3 position, double angleInRadians, Vector3 origin = default) {
        var sinTheta = Math.Sin(angleInRadians);
        var cosTheta = Math.Cos(angleInRadians);

        var translatedX = position.X - origin.X;
        var translatedY = position.Y - origin.Y;

        return new Vector3
        {
            X = (float)(translatedX * cosTheta - translatedY * sinTheta + origin.X),
            Y = (float)(translatedX * sinTheta + translatedY * cosTheta + origin.Y),
            Z = position.Z,
        };
    }
}
