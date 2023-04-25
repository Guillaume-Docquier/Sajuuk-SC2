using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Algorithms;
using Bot.GameData;
using Bot.GameSense.RegionTracking;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.ExtensionMethods;

public static class Vector2Extensions {
    public static Point2D ToPoint2D(this Vector2 vector, float xOffset = 0, float yOffset = 0) {
        return new Point2D
        {
            X = vector.X + xOffset,
            Y = vector.Y + yOffset,
        };
    }

    public static float DistanceTo(this Vector2 origin, Unit unit) {
        return Vector2.Distance(origin, unit.Position.ToVector2());
    }

    public static float DistanceTo(this Vector2 origin, Vector2 destination) {
        return Vector2.Distance(origin, destination);
    }

    // TODO GD Write proper documentation
    // Distance means the radius of the square (it returns diagonal neighbors that are 1.41 units away)
    public static IEnumerable<Vector2> GetNeighbors(this Vector2 vector, int distance = 1) {
        for (var x = -distance; x <= distance; x++) {
            for (var y = -distance; y <= distance; y++) {
                if (x != 0 || y != 0) {
                    yield return vector.Translate(xTranslation: x, yTranslation: y);
                }
            }
        }
    }

    // Center of cells are on .5, e.g: (1.5, 2.5)
    public static Vector2 AsWorldGridCenter(this Vector2 vector) {
        return new Vector2((float)Math.Floor(vector.X) + KnowledgeBase.GameGridCellRadius, (float)Math.Floor(vector.Y) + KnowledgeBase.GameGridCellRadius);
    }

    // Corner of cells are on .0, e.g: (1.0, 2.0)
    public static Vector2 AsWorldGridCorner(this Vector2 vector) {
        return new Vector2((float)Math.Floor(vector.X), (float)Math.Floor(vector.Y));
    }

    /// <summary>
    /// Gets the Region of a given position
    /// </summary>
    /// <param name="position">The position to get the Region of</param>
    /// <returns>The Region of the given position</returns>
    public static IRegion GetRegion(this Vector2 position) {
        return RegionAnalyzer.Instance.GetRegion(position);
    }

    /// <summary>
    /// Gets the force associated with this position
    /// </summary>
    /// <param name="position">The position to get the force of</param>
    /// <param name="alliance">The alliance to get the force of</param>
    /// <returns>A number representing the force. The higher the better.</returns>
    public static float GetForce(this Vector2 position, Alliance alliance) {
        return RegionTracker.GetForce(position, alliance);
    }

    /// <summary>
    /// Gets the value associated with this position
    /// </summary>
    /// <param name="position">The position to get the value of</param>
    /// <param name="alliance">The alliance to get the force of</param>
    /// <returns>A number representing the value. The higher the better.</returns>
    public static float GetValue(this Vector2 position, Alliance alliance) {
        return RegionTracker.GetValue(position, alliance);
    }

    /// <summary>
    /// Gets all cells traversed by the ray from origin to destination using digital differential analyzer (DDA)
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="destination"></param>
    /// <returns>The cells traversed by the ray from origin to destination</returns>
    public static HashSet<Vector2> GetPointsInBetween(this Vector2 origin, Vector2 destination) {
        var targetCellCorner = destination.AsWorldGridCorner();

        var pointsInBetween = RayCasting.RayCast(origin, destination, cellCorner => cellCorner == targetCellCorner)
            .Select(result => result.CornerOfCell.AsWorldGridCenter())
            .ToHashSet();

        return pointsInBetween;
    }

    /// <summary>
    /// Rotates the given position by a certain angle in radians with respect to a given origin, or (0, 0, 0)
    /// </summary>
    /// <param name="position">The position to rotate</param>
    /// <param name="angleInRadians">The angle in radians to rotate by</param>
    /// <param name="origin">The origin to rotate around</param>
    /// <returns>The resulting position</returns>
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
    /// </summary>
    /// <param name="origin">The origin of the translation</param>
    /// <param name="radAngle">The angle to translate towards</param>
    /// <param name="distance">The distance to translate</param>
    /// <returns>A new Vector2 that is translated towards the radAngle by a certain distance</returns>
    public static Vector2 TranslateInDirection(this Vector2 origin, float radAngle, float distance) {
        var translated = origin.Translate(xTranslation: distance);

        return translated.RotateAround(origin, radAngle);
    }

    /// <summary>
    /// Calculates a new vector that is translated towards the destination by a certain distance
    /// </summary>
    /// <param name="origin">The origin of the translation</param>
    /// <param name="destination">The destination to translate towards</param>
    /// <param name="distance">The distance to translate</param>
    /// <returns>A new Vector2 that is translated towards the destination by a certain distance</returns>
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
    /// Calculates the angle between two vectors
    /// The angle is going to be within ]-PI, PI].
    /// </summary>
    /// <param name="v1">The first vector</param>
    /// <param name="v2">The second vector</param>
    /// <returns>The angle in rad between the two vectors within ]-PI, PI]</returns>
    public static double GetRadAngleTo(this Vector2 v1, Vector2 v2) {
        return Math.Acos(Vector2.Dot(v1, v2) / (v1.Length() * v2.Length()));
    }
}
