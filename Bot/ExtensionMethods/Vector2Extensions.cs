using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;
using Bot.GameSense;
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
    /// <para>Gets up to 8 reachable neighbors around the position.</para>
    /// <para>Top, left, down and right are given if they are walkable.</para>
    /// <para>
    /// Diagonal neighbors are returned only if at least one of their components if walkable.
    /// For example, the top right diagonal is reachable of either the top or the right is walkable.
    /// </para>
    /// <para>This is a game detail.</para>
    /// </summary>
    /// <param name="position">The position to get the neighbors of</param>
    /// <param name="includeObstacles">If you're wondering if you should be using this, you shouldn't.</param>
    /// <returns>Up to 8 neighbors</returns>
    public static IEnumerable<Vector2> GetReachableNeighbors(this Vector2 position, bool includeObstacles = true) {
        var leftPos = position.Translate(xTranslation: -1);
        var isLeftOk = MapAnalyzer.IsInBounds(leftPos) && MapAnalyzer.IsWalkable(leftPos, includeObstacles);
        if (isLeftOk) {
            yield return leftPos;
        }

        var rightPos = position.Translate(xTranslation: 1);
        var isRightOk = MapAnalyzer.IsInBounds(rightPos) && MapAnalyzer.IsWalkable(rightPos, includeObstacles);
        if (isRightOk) {
            yield return rightPos;
        }

        var upPos = position.Translate(yTranslation: 1);
        var isUpOk = MapAnalyzer.IsInBounds(upPos) && MapAnalyzer.IsWalkable(upPos, includeObstacles);
        if (isUpOk) {
            yield return upPos;
        }

        var downPos = position.Translate(yTranslation: -1);
        var isDownOk = MapAnalyzer.IsInBounds(downPos) && MapAnalyzer.IsWalkable(downPos, includeObstacles);
        if (isDownOk) {
            yield return downPos;
        }

        if (isLeftOk || isUpOk) {
            var leftUpPos = position.Translate(xTranslation: -1, yTranslation: 1);
            if (MapAnalyzer.IsInBounds(leftUpPos) && MapAnalyzer.IsWalkable(leftUpPos, includeObstacles)) {
                yield return leftUpPos;
            }
        }

        if (isLeftOk || isDownOk) {
            var leftDownPos = position.Translate(xTranslation: -1, yTranslation: -1);
            if (MapAnalyzer.IsInBounds(leftDownPos) && MapAnalyzer.IsWalkable(leftDownPos, includeObstacles)) {
                yield return leftDownPos;
            }
        }

        if (isRightOk || isUpOk) {
            var rightUpPos = position.Translate(xTranslation: 1, yTranslation: 1);
            if (MapAnalyzer.IsInBounds(rightUpPos) && MapAnalyzer.IsWalkable(rightUpPos, includeObstacles)) {
                yield return rightUpPos;
            }
        }

        if (isRightOk || isDownOk) {
            var rightDownPos = position.Translate(xTranslation: 1, yTranslation: -1);
            if (MapAnalyzer.IsInBounds(rightDownPos) && MapAnalyzer.IsWalkable(rightDownPos, includeObstacles)) {
                yield return rightDownPos;
            }
        }
    }

    public static Vector2 ClosestWalkable(this Vector2 position) {
        if (MapAnalyzer.IsWalkable(position)) {
            return position;
        }

        var closestWalkableCell = MapAnalyzer.BuildSearchGrid(position, 15)
            .Where(cell => MapAnalyzer.IsWalkable(cell))
            .DefaultIfEmpty()
            .MinBy(cell => cell.DistanceTo(position));

        // It's probably good to avoid returning default?
        if (closestWalkableCell == default) {
            Logger.Error("Vector3.ClosestWalkable returned no elements in a 15 radius around {0}", position);
            return position;
        }

        return closestWalkableCell;
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
}
