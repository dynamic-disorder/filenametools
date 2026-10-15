namespace FilenameSanitizer;

/// <summary>
///     Represents a simple logging interface for the application.
/// </summary>
public interface ILogger
{
    /// <summary>
    ///     Logs an error message.
    /// </summary>
    /// <param name="message">The error message to log</param>
    void LogError(string message);

    /// <summary>
    ///     Logs an error message with exception details.
    /// </summary>
    /// <param name="message">The error message to log</param>
    /// <param name="ex">The exception to include in the log</param>
    void LogError(string message, Exception ex);

    /// <summary>
    ///     Logs a debug message.
    /// </summary>
    /// <param name="message">The debug message to log</param>
    void LogDebug(string message);
}