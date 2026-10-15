using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FilenameSanitizer;

/// <summary>
/// Provides simple in-memory logging of errors, warnings and informational
/// messages during a filename sanitization operation and the ability to
/// flush those messages to a timestamped log file in a working folder.
/// </summary>
public class OperationLogger
{
    /// <summary>
    /// Template used as the beginning of the log file name. A timestamp and
    /// file extension are appended when the actual log file path is built.
    /// </summary>
    public const string LogFilenameTemplate = "FilenameSanitizer.log.";

    /// <summary>
    /// Gets the timestamp when this logger was created.
    /// </summary>
    public DateTime CreationTime { get; }

    /// <summary>
    /// Gets the formatted timestamp for file names.
    /// </summary>
    public string Timestamp => CreationTime.ToString("yyyy.MM.dd.HH.mm.ss", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the list of header lines to include at the beginning of the log.
    /// </summary>
    public List<string> HeaderLines { get; } = new();

    /// <summary>
    /// Initializes a new instance of <see cref="OperationLogger"/> that will
    /// write log files into <paramref name="workingFolder"/>.
    /// </summary>
    /// <param name="workingFolder">Folder where log files will be written.</param>
    public OperationLogger(string workingFolder)
    {
        WorkingFolder = workingFolder;
        CreationTime = DateTime.Now;

        LogFilePath = Path.Combine(WorkingFolder, $"{LogFilenameTemplate}{Timestamp}.txt");

        // Add default header lines
        HeaderLines.Add("=== FilenameSanitizer Operation Log ===");
        HeaderLines.Add($"Timestamp: {CreationTime:yyyy-MM-dd HH:mm:ss}");
    }

    /// <summary>
    /// List of error messages collected during the operation.
    /// </summary>
    public List<string> Errors { get; } = new();

    /// <summary>
    /// List of warning messages collected during the operation.
    /// </summary>
    public List<string> Warnings { get; } = new();

    /// <summary>
    /// List of informational messages collected during the operation.
    /// </summary>
    public List<string> Info { get; } = new();

    /// <summary>
    /// Gets a value indicating whether any errors were recorded.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// Gets a value indicating whether any warnings were recorded.
    /// </summary>
    public bool HasWarnings => Warnings.Count > 0;

    /// <summary>
    /// Gets a value indicating whether any informational messages were
    /// recorded.
    /// </summary>
    public bool HasInfo => Info.Count > 0;

    /// <summary>
    /// The folder where log files will be written when <see cref="FlushToFile"/>
    /// is called.
    /// </summary>
    public string WorkingFolder { get; }

    /// <summary>
    /// Full path to the log file that will be written for this instance.
    /// </summary>
    public string LogFilePath { get; }

    /// <summary>
    /// Writes the currently collected log entries to the <see cref="LogFilePath"/>.
    /// The method will attempt to create the <see cref="WorkingFolder"/> if it
    /// does not exist. Any exception during writing is captured and added to
    /// the <see cref="Errors"/> collection.
    /// </summary>
    public void FlushToFile()
    {
        var logEntries = FormatLogEntries();

        try
        {
            Directory.CreateDirectory(WorkingFolder);
            File.WriteAllLines(LogFilePath, logEntries);
        }
        catch (Exception ex)
        {
            Errors.Add($"Failed to write log file: {ex.Message}");
        }
    }

    private List<string> FormatLogEntries()
    {
        var entries = new List<string>();

        // Add header lines
        entries.AddRange(HeaderLines);

        // Add empty line after headers
        entries.Add(string.Empty);

        if (HasErrors)
        {
            entries.Add("ERR:");
            entries.AddRange(Errors.Select(e => $"- {e}"));
            entries.Add(string.Empty);
        }

        if (HasWarnings)
        {
            entries.Add("WRN:");
            entries.AddRange(Warnings.Select(w => $"- {w}"));
            entries.Add(string.Empty);
        }

        if (HasInfo)
        {
            entries.Add("INF:");
            entries.AddRange(Info.Select(i => $"- {i}"));
        }

        return entries;
    }
}
