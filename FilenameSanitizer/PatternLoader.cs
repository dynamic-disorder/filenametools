using System.Text.RegularExpressions;

namespace FilenameSanitizer;

/// <summary>
///     Default implementation of IPatternLoader that uses IFileSystem for file operations.
/// </summary>
public class PatternLoader : IPatternLoader
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    ///     Initializes a new instance of the PatternLoader class.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction to use</param>
    public PatternLoader(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    /// <inheritdoc />
    public List<string> LoadReplacementPatterns()
    {
        var replacePatternsSettingsFilename = _fileSystem.Combine(AppDomain.CurrentDomain.BaseDirectory,
            SanitizerConstants.SanitizerReplacePatternsFile);
        if (!_fileSystem.FileExists(replacePatternsSettingsFilename))
        {
            return new List<string>();
        }

        // Read replacement patterns from the file
        return _fileSystem.ReadAllLines(replacePatternsSettingsFilename)
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#')) // Ignore empty lines and comments
            .Select(line => line.Trim())
            .ToList();
    }

    /// <inheritdoc />
    public string ApplyReplacementPatterns(string filename, List<string> patterns, string replacementCharacter)
    {
        foreach (var pattern in patterns)
        {
            filename = filename.Replace(pattern, replacementCharacter);
        }
        return filename;
    }

    /// <inheritdoc />
    public char[] GetInvalidCharacters(ISanitizerSetting settings)
    {
        // Define the invalid characters, including POSIX unsafe chars
        char[] additionalPosixUnsafeChars = [
            '!', '&', '(', ')', '{', '}', '[', ']', '<', '>', '|', '?', '=', '`', '\'', '¨', '~', '^', '*', '@',
            '£', '€', '$', ';', '-', '\0', '/', '\\' // Additional POSIX unsafe characters
        ];
        
        var invalidCharsList = new List<char>();
        
        // Add system invalid file name characters
        foreach (char c in _fileSystem.GetInvalidFilenameChars())
        {
            if (ShouldIncludeCharacter(c, settings))
            {
                invalidCharsList.Add(c);
            }
        }
        
        // Add additional POSIX unsafe characters
        foreach (char c in additionalPosixUnsafeChars)
        {
            if (ShouldIncludeCharacter(c, settings))
            {
                invalidCharsList.Add(c);
            }
        }
        
        return invalidCharsList.ToArray();
    }
    
    /// <inheritdoc />
    public bool ShouldIncludeCharacter(char c, ISanitizerSetting settings)
    {
        string charStr = c.ToString();
        // Exclude if it's the replacement character itself
        if (charStr == settings.ReplacementCharacter)
        {
            return false;
        }
        
        // Exclude if it's in the excluded characters list
        if (settings.ExcludedCharacters.Contains(charStr))
        {
            return false;
        }
        
        return true;
    }
}