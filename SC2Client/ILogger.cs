namespace SC2Client;

public interface ILogger {
    /**
     * Logs a debug message.
     */
    void Debug(string line, params object[] parameters);

    /**
     * Logs an info message.
     */
    void Info(string line, params object[] parameters);

    /**
     * Logs a warning message.
     */
    void Warning(string line, params object[] parameters);

    /**
     * Logs an error message.
     */
    void Error(string error, params object[] parameters);

    /**
     * Logs a success message.
     */
    void Success(string line, params object[] parameters);

    /**
     * Logs an important message.
     */
    void Important(string line, params object[] parameters);

    /**
     * Logs a performance message.
     */
    void Performance(string line, params object[] parameters);

    /**
     * Logs a metric message.
     */
    void Metric(string line, params object[] parameters);

    /**
     * Logs a tag.
     */
    void Tag(string line, params object[] parameters);
}
