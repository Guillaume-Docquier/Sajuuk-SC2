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

    /// <summary>
    /// Gets all cells traversed by a ray from origin to destination.
    /// The exact coordinates of origin and destination will not be respected, but will instead be used to represent the cell.
    /// It returns cells.
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="destination"></param>
    /// <returns>The cells traversed by the ray from origin to destination</returns>
    public static HashSet<Vector2> GetCellsInBetween(this Vector2 origin, Vector2 destination) {
        return RayCasting.RayCastCellToCell(origin, destination)
            .Select(result => result.Cell)
            .ToHashSet();
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

    /// <summary>
    /// Returns the Vector 2 of the center of the given cell.
    /// Center of cells are on .5, e.g: (1.5, 2.5)
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    public static Vector2 AsCellCenter(this Vector2 vector) {
        return new Vector2((float)Math.Floor(vector.X) + 0.5f, (float)Math.Floor(vector.Y) + 0.5f);
    }

    /// <summary>
    /// Returns the Vector 2 of the corner of the given cell.
    /// Corner of cells are on .0, e.g: (1.0, 2.0)
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    public static Vector2 AsCell(this Vector2 vector) {
        return new Vector2((float)Math.Floor(vector.X), (float)Math.Floor(vector.Y));
    }

    /// <summary>
    /// Builds a square search grid composed of all the 1x1 game cells around a center position.
    /// </summary>
    /// <param name="centerPosition">The position to search around.</param>
    /// <param name="gridRadius">The "radius" of the search grid</param>
    /// <returns></returns>
    public static IEnumerable<Vector2> BuildSearchGrid(this Vector2 centerPosition, int gridRadius) {
        var grid = new List<Vector2>();
        for (var x = centerPosition.X - gridRadius; x <= centerPosition.X + gridRadius; x++) {
            for (var y = centerPosition.Y - gridRadius; y <= centerPosition.Y + gridRadius; y++) {
                grid.Add(new Vector2(x, y));
            }
        }

        return grid.OrderBy(position => centerPosition.DistanceTo(position));
    }

    /// <summary>
    /// Builds a circle search area composed of all the 1x1 game cells around a center position.
    /// </summary>
    /// <param name="centerPosition">The position to search around</param>
    /// <param name="searchRadius">The radius of the search area</param>
    /// <returns></returns>
    public static IEnumerable<Vector2> BuildSearchRadius(this Vector2 centerPosition, float searchRadius) {
        return BuildSearchGrid(centerPosition, (int)searchRadius + 1)
            .Where(cell => cell.DistanceTo(centerPosition) <= searchRadius);
    }
}
