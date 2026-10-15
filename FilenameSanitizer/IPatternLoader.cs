namespace FilenameSanitizer;

/// <summary>
///     Provides functionality for loading and applying replacement patterns.
/// </summary>
public interface IPatternLoader
{
    /// <summary>
    ///     Loads replacement patterns from the configured file.
    /// </summary>
    /// <returns>List of patterns to be replaced</returns>
    List<string> LoadReplacementPatterns();

    /// <summary>
    ///     Applies replacement patterns to the filename, replacing each pattern with the replacement character.
    /// </summary>
    /// <param name="filename">The filename to process</param>
    /// <param name="patterns">List of patterns to replace</param>
    /// <param name="replacementCharacter">The character to replace patterns with</param>
    /// <returns>The filename with patterns replaced</returns>
    string ApplyReplacementPatterns(string filename, List<string> patterns, string replacementCharacter);
    
    /// <summary>
    ///     Gets the list of invalid characters to be replaced, based on system invalid characters,
    ///     POSIX unsafe characters, and excludes the replacement character and any excluded characters.
    /// </summary>
    /// <param name="settings">The sanitizer settings</param>
    /// <returns>Array of characters to be replaced</returns>
    char[] GetInvalidCharacters(ISanitizerSetting settings);
    
    /// <summary>
    ///     Determines whether a character should be included in the invalid characters list.
    ///     Excludes the replacement character and any explicitly excluded characters.
    /// </summary>
    /// <param name="c">The character to check</param>
    /// <param name="settings">The sanitizer settings</param>
    /// <returns>True if the character should be considered invalid and replaced</returns>
    bool ShouldIncludeCharacter(char c, ISanitizerSetting settings);
}