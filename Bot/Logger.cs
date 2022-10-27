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
        Console.ForegroundColor = ConsoleColor.Magenta;
        WriteLine("PERF", line, parameters);
        Console.ResetColor();
    }

    public static void Metric(string line, params object[] parameters) {
        Console.ForegroundColor = ConsoleColor.Cyan;
        WriteLine("METRIC", line, parameters);
        Console.ResetColor();
    }

    public static void Debug(string line, params object[] parameters) {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        WriteLine("DEBUG", line, parameters);
        Console.ResetColor();
    }

    public static void Info(string line, params object[] parameters) {
        Console.ForegroundColor = ConsoleColor.White;
        WriteLine("INFO", line, parameters);
        Console.ResetColor();
    }

    public static void Warning(string line, params object[] parameters) {
        Console.ForegroundColor = ConsoleColor.Yellow;
        WriteLine("WARNING", line, parameters);
        Console.ResetColor();
    }

    public static void Error(string line, params object[] parameters) {
        Console.ForegroundColor = ConsoleColor.Red;
        WriteLine("ERROR", line, parameters);
        Console.ResetColor();
    }

    public static void Success(string line, params object[] parameters) {
        Console.ForegroundColor = ConsoleColor.Green;
        WriteLine("SUCCESS", line, parameters);
        Console.ResetColor();
    }

    public static void Tag(string line, params object[] parameters) {
        Console.ForegroundColor = ConsoleColor.Blue;
        WriteLine("TAG", line, parameters);
        Console.ResetColor();
    }
}
