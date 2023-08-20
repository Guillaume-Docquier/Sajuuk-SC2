namespace Sajuuk.Utils;

public static class TimeUtils {
    public const double NormalFramesPerSecond = 16;
    public const double FasterFramesPerSecond = 22.4;

    public static ulong SecsToFrames(int seconds) {
        return SecsToFrames((float)seconds);
    }

    public static ulong SecsToFrames(float seconds) {
        return (ulong)(FasterFramesPerSecond * seconds);
    }

    public static string GetGameTimeString(uint frame) {
        var totalSeconds = (int)(frame / FasterFramesPerSecond);
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;

        return $"{minutes:00}:{seconds:00}";
    }
}
