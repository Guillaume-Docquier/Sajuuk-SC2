namespace Bot.Utils;

public static class TimeUtils {
    public const double FramesPerSecond = 22.4;

    public static ulong SecsToFrames(int seconds) {
        return SecsToFrames((float)seconds);
    }

    public static ulong SecsToFrames(float seconds) {
        return (ulong)(FramesPerSecond * seconds);
    }

    public static string GetGameTimeString() {
        var totalSeconds = (int)(Controller.Frame / FramesPerSecond);
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;

        return $"{minutes:00}:{seconds:00}";
    }
}
