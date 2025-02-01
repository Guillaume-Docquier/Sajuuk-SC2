using System.Numerics;

namespace Algorithms.ExtensionMethods;

public static class Vector2Extensions {
    public static float DistanceTo(this Vector2 origin, Vector2 destination) {
        return Vector2.Distance(origin, destination);
    }

    /// <summary>
    /// Rotates the given position by a certain angle in radians with respect to a given origin, or (0, 0, 0).
    /// A positive angle will result in a counter clockwise rotation.
    /// </summary>
    /// <param name="position">The position to rotate.</param>
    /// <param name="angleInRadians">The angle in radians to rotate by.</param>
    /// <param name="origin">The origin to rotate around.</param>
    /// <returns>The resulting position.</returns>
    public static Vector2 RotateAround(this Vector2 position, Vector2 origin, double angleInRadians) {
        // We round because Math.Cos(Math.PI / 2) == 6.123233995736766E-17
        // Rounding at the 15th decimal makes it 0, and shouldn't affect other results too much
        var sinTheta = Math.Round(Math.Sin(angleInRadians), 15);
        var cosTheta = Math.Round(Math.Cos(angleInRadians), 15);

        // Make the origin (0, 0)
        var translatedX = position.X - origin.X;
        var translatedY = position.Y - origin.Y;

        return new Vector2
        {
            // Restore the origin
            X = (float)(translatedX * cosTheta - translatedY * sinTheta + origin.X),
            Y = (float)(translatedX * sinTheta + translatedY * cosTheta + origin.Y),
        };
    }

    /// <summary>
    /// Calculates a normal vector that represents a direction vector from the origin towards the destination
    /// </summary>
    /// <param name="origin">The origin</param>
    /// <param name="destination">The destination to look at</param>
    /// <returns>A normal vector that represents a direction vector from the origin towards the destination</returns>
    public static Vector2 DirectionTo(this Vector2 origin, Vector3 destination) {
        return origin.DirectionTo(destination.ToVector2());
    }

    /// <summary>
    /// Calculates a normal vector that represents a direction vector from the origin towards the destination
    /// </summary>
    /// <param name="origin">The origin</param>
    /// <param name="destination">The destination to look at</param>
    /// <returns>A normal vector that represents a direction vector from the origin towards the destination</returns>
    public static Vector2 DirectionTo(this Vector2 origin, Vector2 destination) {
        return Vector2.Normalize(destination - origin);
    }

    public static Vector2 Translate(this Vector2 origin, float xTranslation = 0, float yTranslation = 0) {
        return new Vector2 { X = origin.X + xTranslation, Y = origin.Y + yTranslation };
    }

    /// <summary>
    /// Calculates a new vector that is translated by the given distance in the given angle.
    /// A positive angle will result in a counter clockwise rotation.
    /// </summary>
    /// <param name="origin">The origin of the translation.</param>
    /// <param name="radAngle">The angle to translate towards.</param>
    /// <param name="distance">The distance to translate.</param>
    /// <returns>A new Vector2 that is translated towards the radAngle by a certain distance.</returns>
    public static Vector2 TranslateInDirection(this Vector2 origin, float radAngle, float distance) {
        var translated = origin.Translate(xTranslation: distance);

        return translated.RotateAround(origin, radAngle);
    }

    /// <summary>
    /// Calculates a new vector that is translated towards the destination by a certain distance.
    /// </summary>
    /// <param name="origin">The origin of the translation.</param>
    /// <param name="destination">The destination to translate towards.</param>
    /// <param name="distance">The distance to translate.</param>
    /// <returns>A new Vector2 that is translated towards the destination by a certain distance.</returns>
    public static Vector2 TranslateTowards(this Vector2 origin, Vector2 destination, float distance) {
        var direction = origin.DirectionTo(destination);

        return origin + direction * distance;
    }

    /// <summary>
    /// Calculates a new vector that is translated away from the destination by a certain distance
    /// </summary>
    /// <param name="origin">The origin of the translation</param>
    /// <param name="destination">The destination to translate away from</param>
    /// <param name="distance">The distance to translate</param>
    /// <returns>A new Vector2 that is translated away from the destination by a certain distance</returns>
    public static Vector2 TranslateAwayFrom(this Vector2 origin, Vector2 destination, float distance) {
        var direction = origin.DirectionTo(destination);

        return origin - direction * distance;
    }

    /// <summary>
    /// Calculates the angle between two vectors.
    /// The angle is going to be within ]-PI, PI].
    /// A positive angle is counter clockwise.
    /// </summary>
    /// <param name="v1">The first vector.</param>
    /// <param name="v2">The second vector.</param>
    /// <returns>The counter clockwise angle in rad between the two vectors within ]-PI, PI].</returns>
    public static double GetRadAngleTo(this Vector2 v1, Vector2 v2) {
        return Math.Acos(Vector2.Dot(v1, v2) / (v1.Length() * v2.Length()));
    }
}
