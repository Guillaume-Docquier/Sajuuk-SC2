namespace SC2Client;

/**
 * A logger to log whatever messages wherever and however you want.
 */
public interface ILogger {
    /**
     * Logs a debug message.
     */
    void Debug(string line);

    /**
     * Logs an info message.
     */
    void Info(string line);

    /**
     * Logs a warning message.
     */
    void Warning(string line);

    /**
     * Logs an error message.
     */
    void Error(string error);

    /**
     * Logs a success message.
     */
    void Success(string line);

    /**
     * Logs an important message.
     */
    void Important(string line);

    /**
     * Logs a performance message.
     */
    void Performance(string line);

    /**
     * Logs a metric message.
     */
    void Metric(string line);

    /**
     * Logs a tag.
     */
    void Tag(string line);
}
