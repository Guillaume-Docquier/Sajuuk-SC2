using System;
using System.Collections.Generic;
using System.Linq;

namespace Bot.Utils;

public static class MathUtils {
    public static double DegToRad(double degrees) {
        return degrees * Math.PI / 180;
    }

    public static double RadToDeg(double radians) {
        return radians * 180 / Math.PI;
    }

    // TODO GD Code this yourself :rofl: this looks... questionable
    public static IEnumerable<IEnumerable<T>> Combinations<T>(IEnumerable<T> elements, int k)
    {
        if (k == 0) {
            return new[] { Array.Empty<T>() };
        }

        return elements.SelectMany((e, i) => Combinations(elements.Skip(i + 1), k - 1).Select(c => new[] { e }.Concat(c)));
    }

    public static float Normalize(float number, float min, float max) {
        if (max - min < 0.00001f) {
            return min;
        }

        return (number - min) / (max - min);
    }

    public static float LogScale(float number, float min, float max) {
        var logNum = (float)Math.Log2(number + 1);
        var logMin = (float)Math.Log2(min + 1);
        var logMax = (float)Math.Log2(max + 1);

        return (logNum - logMin) / (logMax - logMin);
    }
}
