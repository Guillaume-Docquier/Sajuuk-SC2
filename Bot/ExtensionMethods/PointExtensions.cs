using System.Numerics;
using SC2APIProtocol;

namespace Bot.ExtensionMethods;

public static class PointExtensions {
    public static Vector2 ToVector2(this Point point) {
        return new Vector2
        {
            X = point.X,
            Y = point.Y,
        };
    }

    public static Vector3 ToVector3(this Point point) {
        return new Vector3
        {
            X = point.X,
            Y = point.Y,
            Z = point.Z,
        };
    }

    public static Vector2 ToVector2(this Point2D point) {
        return new Vector2
        {
            X = point.X,
            Y = point.Y,
        };
    }
}
