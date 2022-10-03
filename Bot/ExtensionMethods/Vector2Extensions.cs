using System;
using System.Collections.Generic;
using System.Numerics;
using Bot.GameData;
using Bot.MapKnowledge;

namespace Bot.ExtensionMethods;

public static class Vector2Extensions {
    public static Vector3 ToVector3(this Vector2 vector, bool withWorldHeight = true) {
        var vector3 = new Vector3(vector.X, vector.Y, 0);

        return withWorldHeight ? vector3.WithWorldHeight() : vector3;
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
}
