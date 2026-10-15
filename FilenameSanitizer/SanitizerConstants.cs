namespace FilenameSanitizer;

/// <summary>
///     Constants used in filename sanitization.
/// </summary>
public static class SanitizerConstants
{
    /// <summary>
    ///     Maximum path length for Windows systems.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public const int MaxWindowsPathLength = 260;

    /// <summary>
    ///     Maximum path length for POSIX systems.
    /// </summary>
    public const int MaxPosixNameLength = 255;

    /// <summary>
    ///     Default file name for replacement patterns.
    /// </summary>
    public const string SanitizerReplacePatternsFile = "sanitizer_replace_patterns.txt";

    /// <summary>
    ///     Default file name for sanitizer settings.
    /// </summary>
    public const string SanitizerSettingsFile = "sanitizer_settings.json";

    /// <summary>
    ///     ASCII code for the first printable character (space).
    /// </summary>
    public const int FirstPrintableAsciiChar = 32;

    /// <summary>
    ///     Reserved Windows file names.
    /// </summary>
    public static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON",
        "PRN",
        "AUX",
        "NUL",
        "COM1",
        "COM2",
        "COM3",
        "COM4",
        "LPT1",
        "LPT2",
        "LPT3",
        "LPT4"
    };
}