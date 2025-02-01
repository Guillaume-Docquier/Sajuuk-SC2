namespace SC2Client;

/// <summary>
/// A collections of utility methods to parse and interpret time in a frame based environment.
/// </summary>
public static class TimeUtils {
    /// <summary>
    /// The number of game frames per seconds in a SC2 games at faster speed.
    /// </summary>
    public const double FramesPerSecond = 22.4;

    /// <summary>
    /// Converts a number of seconds into a number of frames.
    /// </summary>
    /// <param name="seconds">The number of seconds to convert.</param>
    /// <returns>The number of frames equivalent to the number of seconds.</returns>
    public static ulong SecsToFrames(int seconds) {
        return SecsToFrames((float)seconds);
    }

    /// <summary>
    /// Converts a number of seconds into a number of frames.
    /// </summary>
    /// <param name="seconds">The number of seconds to convert.</param>
    /// <returns>The number of frames equivalent to the number of seconds.</returns>
    public static ulong SecsToFrames(float seconds) {
        return (ulong)(FramesPerSecond * seconds);
    }

    /// <summary>
    /// Formats a number of frames into a time string.
    /// </summary>
    /// <param name="frame">The frame number to convert into time elapsed since frame 0.</param>
    /// <returns>A string that represents the time elapsed.</returns>
    public static string GetGameTimeString(uint frame) {
        var totalSeconds = (int)(frame / FramesPerSecond);
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;

        return $"{minutes:00}:{seconds:00}";
    }
}
