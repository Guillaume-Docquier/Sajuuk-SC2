using System;
using System.IO;

// ReSharper disable AssignNullToNotNullAttribute

namespace Bot;

public static class Logger {
    private static string _logFile;
    private static bool _stdoutClosed;

    private static void Initialize() {
        _logFile = "Logs/" + DateTime.UtcNow.ToString("yyyy-MM-dd HH.mm.ss") + ".log";
        Directory.CreateDirectory(Path.GetDirectoryName(_logFile));
    }

    private static void WriteLine(string logLevel, string line, params object[] parameters) {
        if (_logFile == null) {
            Initialize();
        }

        var msg = $"[{DateTime.UtcNow.ToString("HH:mm:ss")} | {GetGameTime()} @ {Controller.Frame,5}] {logLevel,7}: {string.Format(line, parameters)}";

        var file = new StreamWriter(_logFile, true);
        file.WriteLine(msg);
        file.Close();
        // do not write to stdout if it is closed (LadderServer on linux)
        if (!_stdoutClosed) {
            try {
                Console.WriteLine(msg, parameters);
            }
            catch {
                _stdoutClosed = true;
            }
        }
    }

    public static void Debug(string line, params object[] parameters) {
        WriteLine("DEBUG", line, parameters);
    }

    public static void Info(string line, params object[] parameters) {
        WriteLine("INFO", line, parameters);
    }

    public static void Warning(string line, params object[] parameters) {
        WriteLine("WARNING", line, parameters);
    }

    public static void Error(string line, params object[] parameters) {
        WriteLine("ERROR", line, parameters);
    }

    private static string GetGameTime() {
        var totalSeconds = (int)(Controller.Frame / Controller.FramesPerSecond);
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;

        return $"{minutes:00}:{seconds:00}";
    }
}
