using System.Collections.Generic;
using System.Numerics;

namespace Bot.ExtensionMethods;

public static class Vector2Extensions {
    public static Vector3 ToVector3(this Vector2 vector) {
        return new Vector3(vector.X, vector.Y, 0);
    }

    public static float DistanceTo(this Vector2 origin, Vector2 destination) {
        return Vector2.Distance(origin, destination);
    }

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
}
