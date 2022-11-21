using System.Linq;

namespace Bot;

// TODO GD Include that in the Logger instead
public static class PerformanceLogger {
    private static int _groupLevel = 1;

    public static void Group(string groupName) {
        Logger.Info($"{GetTabs()}{groupName}");
        _groupLevel++;
    }

    public static void Log(string message) {
        Logger.Performance($"{GetTabs()}{message}");
    }

    public static void GroupEnd() {
        _groupLevel--;
    }

    private static string GetTabs() {
        return string.Join("", Enumerable.Repeat("|\t", _groupLevel));
    }
}
