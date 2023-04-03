using System;
using System.Diagnostics;

namespace Bot.ExtensionMethods;

// https://stackoverflow.com/a/16130260
public static class StopwatchExtensions {
    public static double GetElapsedTimeMs(this Stopwatch stopwatch, int decimals = 2) {
        var timeInSeconds = stopwatch.ElapsedTicks / (double)Stopwatch.Frequency;

        return Math.Round(1e3 * timeInSeconds, decimals);
    }
}
