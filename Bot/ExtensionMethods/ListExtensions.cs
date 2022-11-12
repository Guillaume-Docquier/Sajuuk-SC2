using System;
using System.Collections.Generic;

namespace Bot.ExtensionMethods;

public static class ListExtensions {
    private static readonly Random Rng = new Random();

    public static IList<T> Shuffle<T>(this IList<T> list)
    {
        var n = list.Count;
        while (n > 1) {
            n--;
            var k = Rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }

        return list;
    }
}
