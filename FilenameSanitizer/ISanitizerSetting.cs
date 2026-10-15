namespace FilenameSanitizer;

/// <summary>
///     Represents settings for filename sanitization.
/// </summary>
public interface ISanitizerSetting
{
    /// <summary>
    ///     Gets the character used to replace invalid characters in filenames.
    /// </summary>
    string ReplacementCharacter { get; }

    /// <summary>
    ///     Gets the list of characters that should be excluded from sanitization.
    /// </summary>
    List<string> ExcludedCharacters { get; }

    /// <summary>
    ///     Checks if the settings are empty or uninitialized.
    /// </summary>
    /// <returns>true if settings are empty; otherwise, false</returns>
    bool IsEmpty();
}