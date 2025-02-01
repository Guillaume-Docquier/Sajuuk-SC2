namespace Algorithms.ExtensionMethods;

public static class ListExtensions {
    private static readonly Random Rng = new Random();

    /// <summary>
    /// Shuffles a list in place.
    /// The same list is returned for convenience.
    /// </summary>
    /// <param name="list">The list to shuffle.</param>
    /// <typeparam name="T">The type of the list elements.</typeparam>
    /// <returns>The provided, now shuffled, list.</returns>
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
