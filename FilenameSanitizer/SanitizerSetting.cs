using System.Text.Json.Serialization;

namespace FilenameSanitizer;

/// <summary>
///     Default implementation of sanitizer settings.
/// </summary>
public class SanitizerSetting : ISanitizerSetting
{
    /// <summary>
    ///     Gets the default list of characters to exclude from sanitization.
    /// </summary>
    private static readonly List<string> DefaultExcludedCharacters = new();

    /// <summary>
    ///     Initializes a new instance of the SanitizerSetting class with default values.
    /// </summary>
    public SanitizerSetting()
    {
        ReplacementCharacter = DefaultCharacter;
        ExcludedCharacters = DefaultExcludedCharacters;
    }

    /// <summary>
    ///     Gets the default character used to replace invalid characters in filenames.
    ///     On Windows, it's a space (' '), on Unix/macOS it's an underscore ('_').
    /// </summary>
    public static string DefaultCharacter
    {
        get
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return " "; // Default for Windows
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix ||
                Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                return "-"; // Default for POSIX systems
            }

            return "_";
        }
    }

    /// <summary>
    ///     Gets an empty instance of the sanitizer settings.
    /// </summary>
    public static ISanitizerSetting EmptyInstance => new SanitizerSetting();

    /// <inheritdoc />
    [JsonPropertyName("ReplacementCharacter")]
    public string ReplacementCharacter { get; set; }

    /// <inheritdoc />
    [JsonPropertyName("ExcludedCharacters")]
    public List<string> ExcludedCharacters { get; set; }

    /// <inheritdoc />
    public bool IsEmpty() =>
        string.IsNullOrEmpty(ReplacementCharacter) &&
        (ExcludedCharacters.Count == 0);
}