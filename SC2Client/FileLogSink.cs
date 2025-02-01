namespace SC2Client;

public class FileLogSink : ILogSink {
    private readonly StreamWriter _fileStream;

    public FileLogSink(string logFilePath) {
        // Open a logging file stream
        // For performance reasons, the file stream is kept open
        // For safety reasons, the file stream is set to auto flush
        var logFile = "Logs/" + DateTime.UtcNow.ToString("yyyy-MM-dd HH.mm.ss") + ".log";
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
        _fileStream = new StreamWriter(logFilePath, append: true);
        _fileStream.AutoFlush = true;
    }

    public void Log(string message) {
        _fileStream.WriteLine(message);
    }
}
