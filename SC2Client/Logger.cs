﻿using System.Diagnostics;
using SC2Client.Trackers;

namespace SC2Client;

/// <summary>
/// A logger to log messages to file and, optionally, to stdout
/// </summary>
public class Logger : ILogger {
    private readonly IFrameClock _frameClock;
    private readonly bool _logToStdOut;
    private readonly StreamWriter _fileStream;

    public Logger(IFrameClock frameClock, bool logToStdOut) {
        _frameClock = frameClock;
        _logToStdOut = logToStdOut;

        // Open a logging file stream
        // For performance reasons, the file stream is kept open
        // For safety reasons, the file stream is set to auto flush
        var logFile = "Logs/" + DateTime.UtcNow.ToString("yyyy-MM-dd HH.mm.ss") + ".log";
        Directory.CreateDirectory(Path.GetDirectoryName(logFile)!);
        _fileStream = new StreamWriter(logFile, append: true);
        _fileStream.AutoFlush = true;
    }

    public ILogger CreateNamed(string name) {
        return new NamedLogger(this, name);
    }

    /// <summary>
    /// Logs a message, adding standardized time information and log level.
    /// </summary>
    /// <param name="logLevel">The log level of the message.</param>
    /// <param name="message">The message to log.</param>
    private void WriteLine(string logLevel, string message) {
        var msg = $"[{DateTime.UtcNow:HH:mm:ss} | {TimeUtils.GetGameTimeString(_frameClock.CurrentFrame)} @ {_frameClock.CurrentFrame,5}] {logLevel,7}: {message}";

        _fileStream.WriteLine(msg);

        if (_logToStdOut) {
            Console.WriteLine(msg);
        }
    }

    public void Debug(string message) {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        WriteLine("DEBUG", message);
        Console.ResetColor();
    }

    public void Info(string message) {
        Console.ForegroundColor = ConsoleColor.White;
        WriteLine("INFO", message);
        Console.ResetColor();
    }

    public void Warning(string message) {
        Console.ForegroundColor = ConsoleColor.Yellow;
        WriteLine("WARNING", message);
        Console.ResetColor();
    }

    public void Error(string message) {
        Console.ForegroundColor = ConsoleColor.Red;

        var stackTrace = string.Join(
            Environment.NewLine,
            Environment.StackTrace
                .Split(Environment.NewLine)
                .Skip(2) // Skipping the Environment.StackTrace and Logger.Error calls
                .Take(7)
        );

        WriteLine("ERROR", $"({GetNameOfCallingClass()}) {message}\n{stackTrace}");
        Console.ResetColor();
    }

    public void Success(string message) {
        Console.ForegroundColor = ConsoleColor.Green;
        WriteLine("SUCCESS", message);
        Console.ResetColor();
    }

    public void Important(string message) {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        WriteLine("VIP", message);
        Console.ResetColor();
    }

    public void Performance(string message) {
        Console.ForegroundColor = ConsoleColor.Magenta;
        WriteLine("PERF", message);
        Console.ResetColor();
    }

    public void Metric(string message) {
        Console.ForegroundColor = ConsoleColor.Cyan;
        WriteLine("METRIC", message);
        Console.ResetColor();
    }

    public void Tag(string message) {
        Console.ForegroundColor = ConsoleColor.Blue;
        WriteLine("TAG", message);
        Console.ResetColor();
    }

    // https://stackoverflow.com/questions/48570573/how-to-get-class-name-that-is-calling-my-method
    private static string GetNameOfCallingClass() {
        string? fullName;
        Type? declaringType;
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
