using System.Diagnostics;

namespace SC2Client;

/**
 * Logs messages to file and, optionally, to stdout
 */
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

    private void WriteLine(string logLevel, string line) {
        var msg = $"[{DateTime.UtcNow:HH:mm:ss} | {TimeUtils.GetGameTimeString(_frameClock.CurrentFrame)} @ {_frameClock.CurrentFrame,5}] {logLevel,7}: {line}";

        _fileStream.WriteLine(msg);

        if (_logToStdOut) {
            Console.WriteLine(msg);
        }
    }

    public void Debug(string line) {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        WriteLine("DEBUG", line);
        Console.ResetColor();
    }

    public void Info(string line) {
        Console.ForegroundColor = ConsoleColor.White;
        WriteLine("INFO", line);
        Console.ResetColor();
    }

    public void Warning(string line) {
        Console.ForegroundColor = ConsoleColor.Yellow;
        WriteLine("WARNING", line);
        Console.ResetColor();
    }

    public void Error(string error) {
        Console.ForegroundColor = ConsoleColor.Red;

        var stackTrace = string.Join(
            Environment.NewLine,
            Environment.StackTrace
                .Split(Environment.NewLine)
                .Skip(2) // Skipping the Environment.StackTrace and Logger.Error calls
                .Take(7)
        );

        WriteLine("ERROR", $"({GetNameOfCallingClass()}) {error}\n{stackTrace}");
        Console.ResetColor();
    }

    public void Success(string line) {
        Console.ForegroundColor = ConsoleColor.Green;
        WriteLine("SUCCESS", line);
        Console.ResetColor();
    }

    public void Important(string line) {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        WriteLine("VIP", line);
        Console.ResetColor();
    }

    public void Performance(string line) {
        Console.ForegroundColor = ConsoleColor.Magenta;
        WriteLine("PERF", line);
        Console.ResetColor();
    }

    public void Metric(string line) {
        Console.ForegroundColor = ConsoleColor.Cyan;
        WriteLine("METRIC", line);
        Console.ResetColor();
    }

    public void Tag(string line) {
        Console.ForegroundColor = ConsoleColor.Blue;
        WriteLine("TAG", line);
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
