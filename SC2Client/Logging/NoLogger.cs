namespace SC2Client.Logging;

public class NoLogger : ILogger {
    public ILogger CreateNamed(string name) {
        return this;
    }

    public void Debug(string message) {}

    public void Info(string message) {}

    public void Warning(string message) {}

    public void Error(string message) {}

    public void Success(string message) {}

    public void Important(string message) {}

    public void Performance(string message) {}

    public void Metric(string message) {}

    public void Tag(string message) {}
}
