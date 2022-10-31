using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;
using Bot.MapKnowledge;

namespace Bot.ExtensionMethods;

public static class Vector2Extensions {
    public static Vector3 ToVector3(this Vector2 vector, bool withWorldHeight = true, float zOffset = 0f) {
        var vector3 = new Vector3(vector.X, vector.Y, zOffset);

        return withWorldHeight ? vector3.WithWorldHeight(zOffset) : vector3;
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

    public static Vector2 Translate(this Vector2 origin, float xTranslation = 0, float yTranslation = 0) {
        return new Vector2 { X = origin.X + xTranslation, Y = origin.Y + yTranslation };
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
    public static Region GetRegion(this Vector2 position) {
        return RegionAnalyzer.GetRegion(position);
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
    public static Vector2 Rotate(this Vector2 position, double angleInRadians, Vector2 origin = default) {
        var sinTheta = Math.Sin(angleInRadians);
        var cosTheta = Math.Cos(angleInRadians);

        var translatedX = position.X - origin.X;
        var translatedY = position.Y - origin.Y;

        return new Vector2
        {
            X = (float)(translatedX * cosTheta - translatedY * sinTheta + origin.X),
            Y = (float)(translatedX * sinTheta + translatedY * cosTheta + origin.X),
        };
    }
}
