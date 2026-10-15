namespace FilenameSanitizer;

/// <summary>
///     Interface for sanitizing filenames and ensuring they are valid for use across different operating systems.
///     Main responsibilities:
///     <para>Pure filename sanitization logic</para>
///     <para>Validation of filenames</para>
///     <para>Handling character replacements and restrictions</para>
///     <para>No file system operations</para>
/// </summary>
public interface IFilenameSanitizer
{
    /// <summary>
    ///     Gets the operation logger for tracking filename sanitization operations.
    /// </summary>
    OperationLogger Logger { get; }

    /// <summary>
    ///     Renames files in the working folder to meet OS filename requirements.
    /// </summary>
    void RenameFilesToMeetOsRequirements();

    /// <summary>
    ///     Renames files by removing specified text patterns from filenames.
    /// </summary>
    /// <param name="patterns">New-line separated list of text patterns to remove from filenames</param>
    void RenameFilesRemovingPatterns(string patterns);
}