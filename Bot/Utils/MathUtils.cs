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
}
