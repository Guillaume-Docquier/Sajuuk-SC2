using System;

namespace Bot.Utils;

public static class MathUtils {
    public static double DegToRad(double degrees) {
        return degrees * Math.PI / 180;
    }

    public static double RadToDeg(double radians) {
        return radians * 180 / Math.PI;
    }
}
