using System.Text.RegularExpressions;

namespace FilenameSanitizer;

/// <inheritdoc />
public class Sanitizer : ISanitizer
{
    private readonly ISanitizerSettingsLoader _sanitizerSettingsLoader;
    private readonly IPatternLoader _patternLoader;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    ///     Initializes a new instance of the Sanitizer class.
    /// </summary>
    /// <param name="sanitizerSettings">The settings loader to use for sanitization configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when sanitizerSettings is null.</exception>
    public Sanitizer(ISanitizerSettingsLoader sanitizerSettings)
        : this(sanitizerSettings, new PatternLoader(new FileSystem()), new FileSystem())
    {
    }

    /// <summary>
    ///     Initializes a new instance of the Sanitizer class with custom dependencies.
    /// </summary>
    /// <param name="sanitizerSettings">The settings loader to use for sanitization configuration.</param>
    /// <param name="patternLoader">The pattern loader to use for loading and applying patterns</param>
    /// <exception cref="ArgumentNullException">Thrown when sanitizerSettings or patternLoader is null.</exception>
    public Sanitizer(ISanitizerSettingsLoader sanitizerSettings, IPatternLoader patternLoader)
        : this(sanitizerSettings, patternLoader, new FileSystem())
    {
    }

    /// <summary>
    ///     Initializes a new instance of the Sanitizer class with custom dependencies.
    /// </summary>
    /// <param name="sanitizerSettings">The settings loader to use for sanitization configuration.</param>
    /// <param name="patternLoader">The pattern loader to use for loading and applying patterns</param>
    /// <param name="fileSystem">The file system abstraction to use for path operations</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public Sanitizer(ISanitizerSettingsLoader sanitizerSettings, IPatternLoader patternLoader, IFileSystem fileSystem)
    {
        _sanitizerSettingsLoader = sanitizerSettings ?? throw new ArgumentNullException(nameof(sanitizerSettings));
        _patternLoader = patternLoader ?? throw new ArgumentNullException(nameof(patternLoader));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    /// <inheritdoc />
    public bool IsFilenameSanitized(string? filename)
    {
        // Empty or null filenames are considered sanitized as they'll be handled by optionalFilenameIfEmpty
        if (string.IsNullOrWhiteSpace(filename))
        {
            return true;
        }

        var sanitized = SanitizeFilename(filename);
        return sanitized == filename;
    }

    /// <inheritdoc />
    public string SanitizeFilename(string? filename, string optionalFilenameIfEmpty = "")
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return optionalFilenameIfEmpty;
        }

        var settings = _sanitizerSettingsLoader.LoadFromFile(SanitizerConstants.SanitizerSettingsFile);

        // Remove leading and trailing whitespace
        filename = filename.Trim();

        filename = SanitizeFilenameFromInvalidCharacters(filename, settings);

        // Clean up multiple consecutive underscores
        filename = Regex.Replace(filename, "__+", settings.ReplacementCharacter);

        // Handle spaces and dots
        filename = filename.Trim(); // Remove leading/trailing spaces
        filename = Regex.Replace(filename, @"\s+",
            settings.ReplacementCharacter); // Replace multiple spaces with single underscore
        filename = Regex.Replace(filename, @"\.+", "."); // Replace multiple dots with single dot
        filename = filename.TrimEnd('.'); // Remove trailing dots (invalid in Windows)

        // Remove control characters
        filename = Regex.Replace(filename, @"\p{C}+", string.Empty);

        filename = FilterOutNonPrintableCharacters(filename);
        filename = SanitizeWithSettings(filename);

        filename = TidyFileExtensionFromSpaces(filename);
        filename = TidyFileExtensionFromCharacter('_', filename);

        // Check for Windows reserved names
        filename = IncludeUnderscoreForWindowsReservedNames(filename);

        // Ensure the filename isn't too long
        filename = CutOffTooLong(filename);

        return filename;
    }

    /// <summary>
    ///     Removes spaces before and after file extension dots.
    /// </summary>
    /// <param name="filename">The filename to process</param>
    /// <returns>The filename with cleaned up spaces around extension dots</returns>
    private static string TidyFileExtensionFromSpaces(string filename)
    {
        var hasOccurenceBeforeDot = Regex.IsMatch(filename, @"\s+\.");
        while (hasOccurenceBeforeDot)
        {
            // Remove spaces before the file extension dot
            filename = Regex.Replace(filename, @"\s+\.", ".");
            hasOccurenceBeforeDot = Regex.IsMatch(filename, @"\s+\.");
        }

        var hasOccurenceAfterDot = Regex.IsMatch(filename, @"\.\s+");
        while (hasOccurenceAfterDot)
        {
            // Remove spaces after the file extension dot
            filename = Regex.Replace(filename, @"\.\s+", ".");
            hasOccurenceAfterDot = Regex.IsMatch(filename, @"\.\s+");
        }

        return filename;
    }

    /// <summary>
    ///     Removes specified characters before and after file extension dots.
    /// </summary>
    /// <param name="character">The character to remove</param>
    /// <param name="filename">The filename to process</param>
    /// <returns>The filename with cleaned up characters around extension dots</returns>
    private static string TidyFileExtensionFromCharacter(char character, string filename)
    {
        var hasOccurenceBeforeDot = Regex.IsMatch(filename, character + @"+\.");
        while (hasOccurenceBeforeDot)
        {
            // Remove underscores before the file extension dot
            filename = Regex.Replace(filename, character + @"+\.", ".");
            hasOccurenceBeforeDot = Regex.IsMatch(filename, character + @"+\.");
        }

        var hasOccurenceAfterDot = Regex.IsMatch(filename, @"\." + character + "+");
        while (hasOccurenceAfterDot)
        {
            // Remove underscores after the file extension dot
            filename = Regex.Replace(filename, @"\." + character + "+", ".");
            hasOccurenceAfterDot = Regex.IsMatch(filename, @"\." + character + "+");
        }

        return filename;
    }

    /// <summary>
    ///     Adds an underscore prefix to Windows reserved filenames (CON, PRN, COM1, ...).
    /// </summary>
    /// <remarks>
    ///     Applied on every platform so the produced name stays usable if the file is later
    ///     copied, synced or moved to Windows, which is the point of a cross-platform sanitizer.
    /// </remarks>
    /// <param name="filename">The filename to check</param>
    /// <returns>The filename with an underscore prefix if needed</returns>
    private string IncludeUnderscoreForWindowsReservedNames(string filename)
    {
        var filenameWithoutExt = _fileSystem.GetFilenameWithoutExtension(filename);
        if (SanitizerConstants.ReservedNames.Contains(filenameWithoutExt))
        {
            filename = "_" + filename;
        }

        return filename;
    }

    /// <summary>
    ///     Cuts off the filename if it exceeds the maximum length for POSIX systems.
    /// </summary>
    /// <remarks>File extensions are preserved, but the base filename is truncated if necessary.</remarks>
    /// <param name="filenameInput"></param>
    /// <returns>The original file name if not too long, or the cut-off filename.</returns>
    private string CutOffTooLong(string filenameInput)
    {
        var filename = filenameInput;
        if (filename.Length <= SanitizerConstants.MaxPosixNameLength)
        {
            return filename;
        }

        var extension = _fileSystem.GetExtension(filename);
        var baseFilename = _fileSystem.GetFilenameWithoutExtension(filename);
        try
        {
            filename = baseFilename[..(SanitizerConstants.MaxPosixNameLength - extension.Length)] + extension;
        }
        catch (ArgumentOutOfRangeException exception)
        {
            // 'startIndex' plus 'length' indicates a position not within this instance.
            // -or-
            // 'startIndex' or 'length' is less than zero.
            Console.WriteLine($"Error cutting off filename: {exception.Message}");
            return filenameInput;
        }

        return filename;
    }

    /// <summary>
    ///     Replaces invalid filename characters with the configured replacement character.
    /// </summary>
    /// <param name="filename">The filename to sanitize</param>
    /// <param name="settings">The sanitizer settings to use</param>
    /// <returns>The sanitized filename</returns>
    private string SanitizeFilenameFromInvalidCharacters(string filename, ISanitizerSetting settings)
    {
        var invalidChars = _patternLoader.GetInvalidCharacters(settings);

        // Replace invalid characters with settings.ReplacementCharacter
        filename = invalidChars.Aggregate(filename,
            (current, c) => current.Replace(c.ToString(), settings.ReplacementCharacter));
        filename = CutOffTooLong(filename);
        return filename;
    }

    /// <summary>
    ///     Sanitizes the filename using a default settings file.
    ///     The file should contain replacement patterns in the format "pattern;replacement".
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    private string SanitizeWithSettings(string filename)
    {
        var patterns = _patternLoader.LoadReplacementPatterns();
        if (patterns == null || patterns.Count == 0)
        {
            return filename;
        }

        var settings = _sanitizerSettingsLoader.LoadFromFile(SanitizerConstants.SanitizerSettingsFile);
        return _patternLoader.ApplyReplacementPatterns(filename, patterns, settings.ReplacementCharacter);
    }

    /// <summary>
    ///    Filters out non-printable characters from the filename, keeping only characters that are considered printable in ASCII.
    /// </summary>
    /// <param name="filename">The filename to filter</param>
    /// <returns>The filtered filename</returns>
    private static string FilterOutNonPrintableCharacters(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            return string.Empty; // Return an empty string for null or empty input
        }

        // Filter out non-printable characters by removing any character that has a value less than space
        return new string(filename
            .Where(ch => ch >= SanitizerConstants.FirstPrintableAsciiChar)
            .ToArray());
    }
}