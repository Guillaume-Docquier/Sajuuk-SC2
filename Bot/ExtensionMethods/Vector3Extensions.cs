using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.ExtensionMethods;

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

    public static Vector2 ToVector2(this Vector3 vector) {
        return new Vector2(vector.X, vector.Y);
    }

    public static float DistanceTo(this Vector3 origin, Vector3 destination) {
        return Vector3.Distance(origin, destination);
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

    public static float HorizontalDistanceTo(this Vector3 origin, Unit destination) {
        return origin.HorizontalDistanceTo(destination.Position);
    }

    public static float HorizontalDistanceTo(this Vector3 origin, Vector3 destination) {
        var deltaX = origin.X - destination.X;
        var deltaY = origin.Y - destination.Y;

        return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    // Center of cells are on .5, e.g: (1.5, 2.5)
    public static Vector3 AsWorldGridCenter(this Vector3 vector) {
        return new Vector3((float)Math.Floor(vector.X) + KnowledgeBase.GameGridCellRadius, (float)Math.Floor(vector.Y) + KnowledgeBase.GameGridCellRadius, vector.Z);
    }

    // Corner of cells are on .0, e.g: (1.0, 2.0)
    public static Vector3 AsWorldGridCorner(this Vector3 vector) {
        return new Vector3((float)Math.Floor(vector.X), (float)Math.Floor(vector.Y), vector.Z);
    }

    public static Vector3 WithoutZ(this Vector3 vector) {
        return vector with { Z = 0 };
    }

    public static Vector3 WithWorldHeight(this Vector3 vector, float zOffset = 0) {
        if (!MapAnalyzer.IsInitialized) {
            return vector;
        }

        return vector with { Z = MapAnalyzer.HeightMap[(int)vector.X][(int)vector.Y] + zOffset };
    }

    public static Vector3 ClosestWalkable(this Vector3 position) {
        if (MapAnalyzer.IsWalkable(position)) {
            return position;
        }

        var closestWalkableCell = MapAnalyzer.BuildSearchGrid(position, 15)
            .Where(cell => MapAnalyzer.IsWalkable(cell))
            .DefaultIfEmpty()
            .MinBy(cell => cell.HorizontalDistanceTo(position));

        // It's probably good to avoid returning default?
        if (closestWalkableCell == default) {
            Logger.Error("Vector3.ClosestWalkable returned no elements in a 15 radius around {0}", position);
            return position;
        }

        return closestWalkableCell;
    }

    // TODO GD Write proper documentation
    // Distance means the radius of the square (it returns diagonal neighbors that are 1.41 units away)
    public static IEnumerable<Vector3> GetNeighbors(this Vector3 vector, int radius = 1) {
        for (var x = -radius; x <= radius; x++) {
            for (var y = -radius; y <= radius; y++) {
                if (x != 0 || y != 0) {
                    yield return vector.Translate(xTranslation: x, yTranslation: y);
                }
            }
        }
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
    public static IEnumerable<Vector3> GetReachableNeighbors(this Vector3 position, bool includeObstacles = true) {
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

    /// <summary>
    /// Gets all cells traversed by the ray from origin to destination using digital differential analyzer (DDA)
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="destination"></param>
    /// <returns>The cells traversed by the ray from origin to destination</returns>
    public static HashSet<Vector3> GetPointsInBetween(this Vector3 origin, Vector3 destination) {
        var delta = destination - origin;

        // If delta X is 0, we set the rayLength to a big one so that the Y ray is always chosen (we're moving straight up or straight down)
        var rayLengthWhenMovingInX = delta.Length() + 1;
        if (delta.X != 0) {
            rayLengthWhenMovingInX = (float)Math.Sqrt(1 + (delta.Y / delta.X) * (delta.Y / delta.X));
        }

        // If delta Y is 0, we set the rayLength to a big one so that the X ray is always chosen (we're moving straight left or straight right)
        var rayLengthWhenMovingInY = delta.Length() + 1;
        if (delta.Y != 0) {
            rayLengthWhenMovingInY = (float)Math.Sqrt(1 + (delta.X / delta.Y) * (delta.X / delta.Y));
        }

        // Edge case, if deltaX is 0, stepXDistance can be 0, making the first ray 0, thus it's going to be picked
        // We want to avoid that so we set it to 1
        var stepX = 1;
        var stepXDistance = 1f;
        if (delta.X > 0) {
            // Moving right
            stepX = 1;
            stepXDistance = (float)Math.Floor(origin.X + 1) - origin.X;
        }
        else if (delta.X < 0) {
            // Moving left
            stepX = -1;
            stepXDistance = origin.X - (float)Math.Floor(origin.X);
        }

        // Edge case, if deltaY is 0, stepYDistance can be 0, making the first ray 0, thus it's going to be picked
        // We want to avoid that so we set it to 1
        var stepY = 1;
        var stepYDistance = 1f;
        if (delta.Y > 0) {
            // Moving up
            stepY = 1;
            stepYDistance = (float)Math.Floor(origin.Y + 1) - origin.Y;
        }
        else if (delta.Y < 0) {
            // Moving down
            stepY = -1;
            stepYDistance = origin.Y - (float)Math.Floor(origin.Y);
        }

        var currentCell = origin.AsWorldGridCorner();
        var targetCell = destination.AsWorldGridCorner();
        var xRayLength = stepXDistance * rayLengthWhenMovingInX;
        var yRayLength = stepYDistance * rayLengthWhenMovingInY;

        var pointsInBetween = new HashSet<Vector3> { currentCell.AsWorldGridCenter() };
        while (currentCell != targetCell) {
            if (xRayLength < yRayLength) {
                // Step in X, reduce Y ray
                yRayLength -= xRayLength;

                // Move to the cell on the left or right
                currentCell.X += stepX;

                // Reset X ray
                xRayLength = rayLengthWhenMovingInX;
            }
            else if (yRayLength < xRayLength) {
                // Step in Y, reduce X ray
                xRayLength -= yRayLength;

                // Move to the cell on the bottom or top
                currentCell.Y += stepY;

                // Reset Y ray
                yRayLength = rayLengthWhenMovingInY;
            }
            else {
                // Both rays are the same, means we landed exactly on a corner
                currentCell.X += stepX; // Move to the left/right
                currentCell.Y += stepY; // And up/down

                // Reset all rays
                xRayLength = rayLengthWhenMovingInX;
                yRayLength = rayLengthWhenMovingInY;
            }

            pointsInBetween.Add(currentCell.AsWorldGridCenter());
        }

        return pointsInBetween;
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
            Y = (float)(translatedX * sinTheta + translatedY * cosTheta + origin.X),
        };
    }

    /// <summary>
    /// Gets the Region of a given position
    /// </summary>
    /// <param name="position">The position to get the Region of</param>
    /// <returns>The Region of the given position</returns>
    public static Region GetRegion(this Vector3 position) {
        return RegionAnalyzer.GetRegion(position);
    }

    /// <summary>
    /// Gets the danger level associated with this position
    /// </summary>
    /// <param name="position">The position to get the danger level of</param>
    /// <returns>A number representing the danger level. Positive is dangerous, 0 is considered neutral and negative is safe</returns>
    public static float GetDangerLevel(this Vector3 position) {
        return RegionTracker.GetDangerLevel(position);
    }
}
