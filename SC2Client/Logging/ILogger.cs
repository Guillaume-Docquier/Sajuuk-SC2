namespace SC2Client.Logging;

/// <summary>
/// An interface to log messages of various types.
/// The different log levels can be used to give more or less importance to certain logs.
/// They can also be used to parse and analyze certain categories of logs.
/// </summary>
public interface ILogger {
    /// <summary>
    /// Creates an instance of this logger with a name prefix added to all messages.
    /// </summary>
    /// <param name="name">The name prefix to use.</param>
    /// <returns></returns>
    ILogger CreateNamed(string name);

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message">The debug message.</param>
    void Debug(string message);

    /// <summary>
    /// Logs an info message.
    /// </summary>
    /// <param name="message">The info message.</param>
    void Info(string message);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The warning message.</param>
    void Warning(string message);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    void Error(string message);

    /// <summary>
    /// Logs a success message.
    /// Success messages are more visible.
    /// </summary>
    /// <param name="message">The success message.</param>
    void Success(string message);

    /// <summary>
    /// Logs an important message.
    /// Important messages are more visible.
    /// </summary>
    /// <param name="message">The important message.</param>
    void Important(string message);

    /// <summary>
    /// Logs a performance message.
    /// Performance messages are used to keep track of potential performance problems.
    /// </summary>
    /// <param name="message">The performance message.</param>
    void Performance(string message);

    /// <summary>
    /// Logs a metric message.
    /// Metrics are used as comparative tokens from game to game.
    /// </summary>
    /// <param name="message">The metric message.</param>
    void Metric(string message);

    /// <summary>
    /// Logs a message related to AIArena tags.
    /// </summary>
    /// <param name="message">The tag to log.</param>
    void Tag(string message);
}
