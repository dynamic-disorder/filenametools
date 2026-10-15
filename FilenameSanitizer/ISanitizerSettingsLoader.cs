namespace FilenameSanitizer;

/// <summary>
///     Interface for loading sanitizer settings from files.
/// </summary>
public interface ISanitizerSettingsLoader
{
    /// <summary>
    ///     Loads sanitizer settings from a specified file.
    /// </summary>
    /// <param name="sanitizerSettingsFile">The settings file to load</param>
    /// <returns>The loaded sanitizer settings</returns>
    ISanitizerSetting LoadFromFile(string sanitizerSettingsFile);
}