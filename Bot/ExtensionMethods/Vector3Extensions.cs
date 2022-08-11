using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;
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

    public static Vector3 WithWorldHeight(this Vector3 vector) {
        return vector with { Z = MapAnalyzer.HeightMap[(int)vector.X][(int)vector.Y] };
    }

    public static Vector3 ClosestWalkable(this Vector3 vector) {
        if (MapAnalyzer.IsWalkable(vector)) {
            return vector;
        }

        var closestWalkableCell = MapAnalyzer.BuildSearchGrid(vector, 10)
            .Where(MapAnalyzer.IsWalkable)
            .MinBy(cell => cell.HorizontalDistanceTo(vector));

        // It's probably good to avoid returning default
        if (closestWalkableCell == default) {
            return vector;
        }

        return closestWalkableCell;
    }

    public static IEnumerable<Vector3> GetNeighbors(this Vector3 vector) {
        yield return vector.Translate(xTranslation: -1, yTranslation: 1);
        yield return vector.Translate(xTranslation: -1, yTranslation: 0);
        yield return vector.Translate(xTranslation: -1, yTranslation: -1);

        yield return vector.Translate(xTranslation: 0, yTranslation: 1);
        yield return vector.Translate(xTranslation: 0, yTranslation: -1);

        yield return vector.Translate(xTranslation: 1, yTranslation: 1);
        yield return vector.Translate(xTranslation: 1, yTranslation: 0);
        yield return vector.Translate(xTranslation: 1, yTranslation: -1);
    }
}
