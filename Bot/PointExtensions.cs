using System.Numerics;
using SC2APIProtocol;

namespace Bot;

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
            Z = Pathfinder.HeightMap[(int)point.X][(int)point.Y],
        };
    }
}
