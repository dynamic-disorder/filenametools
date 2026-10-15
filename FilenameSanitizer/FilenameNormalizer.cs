using System;
using System.Linq;
using System.Text.Json;

namespace FilenameSanitizer;

/// <inheritdoc />
public class FilenameNormalizer : IFilenameSanitizer
{
    private readonly IFileSystem _fileSystem;
    private readonly string _folder;
    private readonly ISanitizer _sanitizer;


    /// <summary>
    ///     Initializes a new instance of the FilenameSanitizer class.
    /// </summary>
    /// <param name="folder">Optional working folder. If not specified, uses current directory.</param>
    /// <param name="sanitizer"></param>
    /// <param name="fileSystem">Optional file system implementation. If not specified, uses default file system.</param>
    /// <param name="logger">Optional logger implementation. If not specified, uses console logger.</param>
    public FilenameNormalizer(string? folder = null, ISanitizer? sanitizer = null, IFileSystem? fileSystem = null,
        ILogger? logger = null)
    {
        _folder = ResolveFolderPath(folder);
        Logger = new OperationLogger(_folder);
        _fileSystem = fileSystem ?? new FileSystem();
        var effectiveLogger = logger ?? new ConsoleLogger();

        // Set up log information about sanitizer settings and replacement patterns
        var sanitizerSettingsInfo = GetSanitizerSettingsInfo(effectiveLogger);
        Logger.HeaderLines.Add(sanitizerSettingsInfo);

        var replacementPatternsCountInfo = GetReplacementPatternsCountInfo();
        Logger.HeaderLines.Add(replacementPatternsCountInfo);

        if (sanitizer == null)
        {
            var settingsLoader = new SanitizerSettingsLoader(_fileSystem, effectiveLogger);
            _sanitizer = new Sanitizer(settingsLoader);
        }
        else
        {
            _sanitizer = sanitizer;
        }
    }

    /// <inheritdoc />
    public OperationLogger Logger { get; }

    /// <inheritdoc />
    public void RenameFilesToMeetOsRequirements()
    {
        var filePaths = GetFilePathsFromWorkingFolder();

        Logger.Info.Add("Starting to sanitize filenames in folder: " + _folder);
        IEnumerable<string> enumerable = filePaths.ToList();
        if (!enumerable.Any())
        {
            return;
        }

        Logger.Info.Add($"Found {enumerable.Count()} files in folder: {_folder}");
        foreach (var filePath in enumerable)
        {
            Logger.Info.Add($"Processing file: {filePath}");
            RenameFileIfNeeded(filePath);
        }
    }

    /// <inheritdoc />
    public void RenameFilesRemovingPatterns(string patterns)
    {
        Logger.Info.Add($"Removing patterns from filenames in folder: {_folder}");
        var files = GetFilePathsFromWorkingFolder();
        if (files.Any())
        {
            var patternsList = patterns.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var file in files)
            {
                Logger.Info.Add($"Processing file: {file} with patters {patternsList}");
                RenameFileRemovingPatternsIfNeeded(file, patternsList, Logger);
            }
        }
    }

    /// <summary>
    ///     Resolves the working folder path.
    /// </summary>
    /// <param name="folder">Input folder path, can be null, relative or absolute.</param>
    /// <returns>Absolute path to the working folder.</returns>
    private static string ResolveFolderPath(string? folder)
    {
#pragma warning disable IDE0046
        if (!string.IsNullOrWhiteSpace(folder))
#pragma warning restore IDE0046
        {
            return Path.GetFullPath(folder);
        }

        return Environment.CurrentDirectory;
    }

    private IEnumerable<string> GetFilePathsFromWorkingFolder()
    {
        if (!_fileSystem.DirectoryExists(_folder))
        {
            Logger.Errors.Add($"Folder '{_folder}' does not exist.");
            return Enumerable.Empty<string>();
        }

        var allFilePaths = _fileSystem.GetFiles(_folder);
        var filePaths = new List<string>();
        foreach (var filePath in allFilePaths)
        {
            if (IsReservedName(filePath))
            {
                var filename = Path.GetFileName(filePath);

                Logger.Info.Add($"Ignoring reserved file name: {filename}");
                continue;
            }

            filePaths.Add(filePath);
        }

        return filePaths;
    }

    private static bool IsReservedName(string filePath)
    {
        var filename = Path.GetFileName(filePath);
        if (filename == SanitizerConstants.SanitizerReplacePatternsFile)
        {
            return true;
        }

        if (filename == SanitizerConstants.SanitizerSettingsFile)
        {
            return true;
        }

#pragma warning disable IDE0046
        if (!filename.StartsWith(OperationLogger.LogFilenameTemplate, StringComparison.OrdinalIgnoreCase))
#pragma warning restore IDE0046
        {
            return false;
        }

        return true;
    }

    private void RenameFileIfNeeded(string filePath)
    {
        var sanitizedFilename = GetSanitizedFilenameWithPathRemoved(filePath);

        if (!ShouldRenameFile(filePath, sanitizedFilename))
        {
            Logger.Info.Add($"Skipping file: {filePath}");
            return;
        }

        TryRenameFile(filePath, sanitizedFilename);
    }

    private void TryRenameFile(string originalFile, string sanitizedFilename)
    {
        var newFilePath = Path.Combine(_folder, sanitizedFilename);

        if (_fileSystem.FileExists(newFilePath))
        {
            Logger.Errors.Add($"File '{newFilePath}' already exists. Skipping rename for '{originalFile}'.");
            return;
        }

        try
        {
            _fileSystem.MoveFile(originalFile, newFilePath);
        }
        catch (Exception ex)
        {
            Logger.Errors.Add($"Error renaming file '{originalFile}' to '{newFilePath}': {ex.Message}");
        }
    }

    private string GetSanitizedFilenameWithPathRemoved(string filePath)
    {
        var filename = Path.GetFileName(filePath);
        Logger.Info.Add($"Sanitizing filename: {filename}");

        var sanitizedFilename = _sanitizer.SanitizeFilename(filename);
        Logger.Info.Add($"Sanitized filename: {sanitizedFilename}");

        return sanitizedFilename;
    }

    private bool ShouldRenameFile(string originalFile, string sanitizedFilename)
    {
        if (string.IsNullOrWhiteSpace(sanitizedFilename))
        {
            Logger.Errors.Add($"File '{originalFile}' has an empty sanitized filename. Skipping.");
            return false;
        }

        var originalFilename = Path.GetFileName(originalFile);
        if (_sanitizer.IsFilenameSanitized(originalFilename))
        {
            Logger.Warnings.Add($"File '{originalFilename}' is already sanitized. Skipping.");
            return false;
        }

        return true;
    }

    private void RenameFileRemovingPatternsIfNeeded(string file, string[] patterns, OperationLogger logger)
    {
        var filename = Path.GetFileName(file);
        var sanitizedFilename = RemovePatternsFromFilename(filename, patterns);

        if (!ShouldRenameFile(file, sanitizedFilename))
        {
            Logger.Info.Add($"Skipping file: {file}");
            return;
        }

        TryRenameFile(file, sanitizedFilename);
    }

    private string RemovePatternsFromFilename(string filename, string[] patterns)
    {
        Logger.Info.Add($"Removing patterns from filename: {filename}");
        var filenamePatternRemoved = patterns.Aggregate(filename, (currentSeed, pattern) =>
        {
            var filenameProcessed = currentSeed.Replace(pattern, string.Empty, StringComparison.OrdinalIgnoreCase);
            return filenameProcessed;
        });

        Logger.Info.Add($"Filename after removing patterns: {filenamePatternRemoved}");
        return filenamePatternRemoved;
    }

    private string GetSanitizerSettingsInfo(ILogger logger)
    {
        try
        {
            var settingsLoader = new SanitizerSettingsLoader(_fileSystem, logger);
            var settings = settingsLoader.LoadFromFile(SanitizerConstants.SanitizerSettingsFile);
            var excludedChars = settings.ExcludedCharacters != null 
                ? string.Join(", ", settings.ExcludedCharacters.Select(c => $"\"{c}\""))
                : "none";
            return $"sanitizer_settings.json: ReplacementCharacter = \"{settings.ReplacementCharacter}\", ExcludedCharacters = [{excludedChars}]";
        }
        catch (Exception)
        {
            return "Settings file not found or could not be parsed.";
        }
    }

    private string GetReplacementPatternsCountInfo()
    {
        try
        {
            var patternLoader = new PatternLoader(_fileSystem);
            var patterns = patternLoader.LoadReplacementPatterns();
            return $"sanitizer_replace_patterns.txt: {patterns.Count} replacement pattern lines";
        }
        catch (Exception)
        {
            return "Replacement patterns file not found or could not be read.";
        }
    }
}
