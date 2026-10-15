namespace FilenameSanitizer;

/// <summary>
///     A simple console-based logger implementation.
///     TODO: Consider replacing with a proper logging framework and introduce log levels.
/// </summary>
public class ConsoleLogger : ILogger
{
    /// <inheritdoc />
    public void LogError(string message)
    {
        Console.WriteLine($"[ERROR] {message}");
    }

    /// <inheritdoc />
    public void LogError(string message, Exception ex)
    {
        Console.WriteLine($"[ERROR] {message}: {ex.Message}");
    }

    /// <inheritdoc />
    public void LogDebug(string message)
    {
        Console.WriteLine($"[DEBUG] {message}");
    }
}