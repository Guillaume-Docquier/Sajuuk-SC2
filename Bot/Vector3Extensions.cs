using System.Numerics;
using SC2APIProtocol;

namespace Bot;

public static class Vector3Extensions {
    public static Point ToPoint(this Vector3 vector) {
        return new Point { X = vector.X, Y = vector.Y, Z = vector.Z };
    }

    public static Point2D ToPoint2D(this Vector3 vector) {
        return new Point2D { X = vector.X, Y = vector.Y };
    }
}
