namespace SC2Client.Logging;

/// <summary>
/// A logger that prefixes all messages with a name.
/// Useful to track which part of the software says what.
/// </summary>
public class NamedLogger : ILogger {
    private readonly ILogger _logger;
    private readonly string _prefix;

    public NamedLogger(ILogger logger, string name) {
        _logger = logger;
        _prefix = $"({name})";
    }

    public ILogger CreateNamed(string name) {
        return new NamedLogger(_logger, name);
    }

    public void Debug(string message) {
        _logger.Debug($"{_prefix} {message}");
    }

    public void Info(string message) {
        _logger.Info($"{_prefix} {message}");
    }

    public void Warning(string message) {
        _logger.Warning($"{_prefix} {message}");
    }

    public void Error(string message) {
        _logger.Error($"{_prefix} {message}");
    }

    public void Success(string message) {
        _logger.Success($"{_prefix} {message}");
    }

    public void Important(string message) {
        _logger.Important($"{_prefix} {message}");
    }

    public void Performance(string message) {
        _logger.Performance($"{_prefix} {message}");
    }

    public void Metric(string message) {
        _logger.Metric($"{_prefix} {message}");
    }

    public void Tag(string message) {
        _logger.Tag($"{_prefix} {message}");
    }
}
