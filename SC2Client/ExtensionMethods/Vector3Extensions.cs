using System.Numerics;
using Algorithms.ExtensionMethods;
using SC2APIProtocol;
using SC2Client.GameData;
using SC2Client.GameState;

namespace SC2Client.ExtensionMethods;

public static class Vector3Extensions {
    public static Point ToPoint(this Vector3 vector, float xOffset = 0, float yOffset = 0, float zOffset = 0) {
        return new Point
        {
            X = vector.X + xOffset,
            Y = vector.Y + yOffset,
            Z = vector.Z + zOffset,
        };
    }

    public static Point2D ToPoint2D(this Vector3 vector, float xOffset = 0, float yOffset = 0) {
        return new Point2D
        {
            X = vector.X + xOffset,
            Y = vector.Y + yOffset,
        };
    }

    public static float HorizontalDistanceTo(this Vector3 origin, IUnit destination) {
        return origin.HorizontalDistanceTo(destination.Position);
    }

    // Center of cells are on .5, e.g: (1.5, 2.5)
    public static Vector3 AsWorldGridCenter(this Vector3 vector) {
        return new Vector3((float)Math.Floor(vector.X) + KnowledgeBase.GameGridCellRadius, (float)Math.Floor(vector.Y) + KnowledgeBase.GameGridCellRadius, vector.Z);
    }

    // Corner of cells are on .0, e.g: (1.0, 2.0)
    public static Vector3 AsWorldGridCorner(this Vector3 vector) {
        return new Vector3((float)Math.Floor(vector.X), (float)Math.Floor(vector.Y), vector.Z);
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
}
