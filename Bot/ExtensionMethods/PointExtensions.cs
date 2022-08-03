using System.Numerics;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.ExtensionMethods;

public static class PointExtensions {
    public static Vector3 ToVector3(this Point point) {
        return new Vector3
        {
            X = point.X,
            Y = point.Y,
            Z = point.Z,
        };
    }

    public static Vector3 ToVector3(this Point2D point) {
        return new Vector3
        {
            X = point.X,
            Y = point.Y,
            Z = MapAnalyzer.HeightMap[(int)point.X][(int)point.Y],
        };
    }
}
