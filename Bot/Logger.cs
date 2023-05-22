using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Bot.Utils;

namespace Bot;

// TODO GD Turn Logger into an injected instance
public static class Logger {
    private static bool _isDisabled = false;
    private static IFrameClock _frameClock;

    private static StreamWriter _fileStream;
    private static bool _stdoutOpen = true;

    // Mostly used for tests
    public static void Disable() {
        _isDisabled = true;
    }

    public static void SetFrameClock(IFrameClock frameClock) {
        _frameClock = frameClock;
    }

    /// <summary>
    /// Open a logging file stream
    /// For performance reasons, the file stream is kept open
    /// For safety reasons, the file stream is set to auto flush
    /// </summary>
    private static void Initialize() {
        var logFile = "Logs/" + DateTime.UtcNow.ToString("yyyy-MM-dd HH.mm.ss") + ".log";
        Directory.CreateDirectory(Path.GetDirectoryName(logFile)!);

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

        var msg = $"[{DateTime.UtcNow:HH:mm:ss} | {TimeUtils.GetGameTimeString(_frameClock.CurrentFrame)} @ {_frameClock.CurrentFrame,5}] {logLevel,7}: {string.Format(line, parameters)}";

        _fileStream!.WriteLine(msg);

        // TODO GD This is a use case for different logger instances based on mode (ladder vs local)
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

    public static void Error(string error, params object[] parameters) {
        Console.ForegroundColor = ConsoleColor.Red;

        var stackTrace = string.Join(
            Environment.NewLine,
            Environment.StackTrace
                .Split(Environment.NewLine)
                .Skip(2) // Skipping the Environment.StackTrace and Logger.Error calls
                .Take(7)
        );

        WriteLine("ERROR", $"({GetNameOfCallingClass()}) {error}\n{stackTrace}", parameters);
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

    public static void Important(string line, params object[] parameters) {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        WriteLine("VIP", line, parameters);
        Console.ResetColor();
    }

    // https://stackoverflow.com/questions/48570573/how-to-get-class-name-that-is-calling-my-method
    private static string GetNameOfCallingClass() {
        string fullName;
        Type declaringType;
        var skipFrames = 2;
        do {
            var method = new StackFrame(skipFrames, false).GetMethod()!;
            declaringType = method.DeclaringType;
            if (declaringType == null) {
                return method.Name;
            }
            skipFrames++;
            fullName = declaringType.FullName;
        }
        while (declaringType.Module.Name.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase));

        if (fullName == null) {
            return "UNKNOWN CALLER";
        }

        // Remove the namespaces, just keep the class name
        return fullName.Split(".").Last();
    }
}
