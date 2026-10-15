using System.Text.Json;

namespace FilenameSanitizer;

/// <summary>
///     Default implementation for loading sanitizer settings from JSON files.
/// </summary>
public class SanitizerSettingsLoader : ISanitizerSettingsLoader
{
    private readonly string _baseDirectory;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;

    /// <summary>
    ///     Initializes a new instance of the SanitizerSettingsLoader class.
    /// </summary>
    /// <param name="fileSystem">The file system to use for file operations</param>
    /// <param name="logger">The logger to use for logging</param>
    /// <param name="baseDirectory">Optional base directory. Defaults to current AppDomain's base directory</param>
    /// <exception cref="ArgumentNullException">Thrown when fileSystem or logger is null</exception>
    public SanitizerSettingsLoader(IFileSystem fileSystem, ILogger logger, string? baseDirectory = null)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _baseDirectory = baseDirectory ?? AppDomain.CurrentDomain.BaseDirectory;
    }

    private static readonly JsonSerializerOptions CaseInsensitiveOptions =
        new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc />
    public ISanitizerSetting LoadFromFile(string sanitizerSettingsFile)
    {
        var filePath = Path.Combine(_baseDirectory, sanitizerSettingsFile);
        if (!_fileSystem.FileExists(filePath))
        {
            _logger.LogError($"File '{sanitizerSettingsFile}' containing settings does not exist at '{filePath}'. Returning an empty {nameof(SanitizerSetting)} instance.");
            return SanitizerSetting.EmptyInstance; // Return default settings if file does not exist
        }

        ISanitizerSetting settings = DeserializeSettings(filePath);
        return settings;
    }

    /// <summary>
    ///    Deserializes the sanitizer settings from the specified file path. If the file does not exist or deserialization fails, returns default settings.
    /// </summary>
    /// <param name="filePath">The path to the settings file</param>
    /// <returns>The deserialized settings or default settings if deserialization fails</returns>
    public ISanitizerSetting DeserializeSettings(string filePath)
    {
        var settings = new SanitizerSetting();

        try
        {
            var jsonContent = _fileSystem.ReadAllText(filePath);
            var deserializedSettings = JsonSerializer.Deserialize<SanitizerSetting>(jsonContent, CaseInsensitiveOptions);
            if (deserializedSettings == null || deserializedSettings.IsEmpty())
            {
                _logger.LogError($"Deserialization of settings from {filePath} returned null for JSON: " + jsonContent);
                return SanitizerSetting.EmptyInstance; // Return empty instance if deserialization fails
            }

            settings = deserializedSettings; // Use deserialized settings or default if deserialization fails
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading settings from {filePath}", ex);
        }

        return settings;
    }
}