namespace FilenameSanitizer;

/// <summary>
///     Interface for filename sanitization functionality.
///     Main responsibilities:
///     <para>Pure filename sanitization logic</para>
///     <para>Validation of filenames</para>
///     <para>Handling character replacements and restrictions</para>
///     <para>No file system operations</para>
/// </summary>
public interface ISanitizer
{
    /// <summary>
    ///     Sanitizes a filename to ensure it meets OS requirements.
    /// </summary>
    string SanitizeFilename(string filename, string optionalFilenameIfEmpty = "");

    /// <summary>
    ///     Gets whether a filename is already properly sanitized.
    /// </summary>
    bool IsFilenameSanitized(string filename);
}