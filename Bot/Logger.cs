using System;
using System.IO;

// ReSharper disable AssignNullToNotNullAttribute

namespace Bot;

public static class Logger {
    private static bool _isDisabled = false;
    private static StreamWriter _fileStream;
    private static bool _stdoutOpen = true;

    // Mostly used for tests
    public static void Disable() {
        _isDisabled = true;
    }

    /// <summary>
    /// Open a logging file stream
    /// For performance reasons, the file stream is kept open
    /// For safety reasons, the file stream is set to auto flush
    /// </summary>
    private static void Initialize() {
        var logFile = "Logs/" + DateTime.UtcNow.ToString("yyyy-MM-dd HH.mm.ss") + ".log";
        Directory.CreateDirectory(Path.GetDirectoryName(logFile));

        _fileStream = new StreamWriter(logFile, append: true);
        _fileStream.AutoFlush = true;
    }

    private static void WriteLine(string logLevel, string line, params object[] parameters) {
        if (_isDisabled) {
            return;
        }

        if (_fileStream == null) {
            Initialize();
        }

        var msg = $"[{DateTime.UtcNow.ToString("HH:mm:ss")} | {Controller.GetGameTimeString()} @ {Controller.Frame,5}] {logLevel,7}: {string.Format(line, parameters)}";

        _fileStream!.WriteLine(msg);

        // Only write to stdout if it is open (typically in dev)
        if (_stdoutOpen) {
            try {
                Console.WriteLine(msg, parameters);
            }
            catch {
                _stdoutOpen = false;
            }
        }
    }

    public static void Performance(string line, params object[] parameters) {
        WriteLine("PERF", line, parameters);
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
}
