namespace FilenameSanitizer;

/// <summary>
///     Represents a file system abstraction for file and directory operations.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    ///     Determines whether the specified path refers to an existing directory.
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <returns>true if path refers to an existing directory; otherwise, false</returns>
    bool DirectoryExists(string path);

    /// <summary>
    ///     Determines whether the specified file exists.
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <returns>true if path refers to an existing file; otherwise, false</returns>
    bool FileExists(string path);

    /// <summary>
    ///     Returns the names of files in the specified directory.
    /// </summary>
    /// <param name="path">The directory to search</param>
    /// <returns>An array of file names in the directory</returns>
    string[] GetFiles(string path);

    /// <summary>
    ///     Moves a specified file to a new location.
    /// </summary>
    /// <param name="sourceFilename">The name of the file to move</param>
    /// <param name="destFilename">The new path for the file</param>
    void MoveFile(string sourceFilename, string destFilename);

    /// <summary>
    ///     Opens a text file, reads all lines of the file, and then closes the file.
    /// </summary>
    /// <param name="path">The file to read</param>
    /// <returns>A string containing all lines of the file</returns>
    string ReadAllText(string path);

    /// <summary>
    ///     Opens a text file, reads all lines of the file, and then closes the file.
    /// </summary>
    /// <param name="path">The file to read</param>
    /// <returns>An array of strings containing all lines of the file</returns>
    string[] ReadAllLines(string path);

    /// <summary>
    ///     Gets the invalid characters for file names in the current operating system.
    /// </summary>
    /// <returns>An array containing the characters that are not allowed in file names</returns>
    char[] GetInvalidFilenameChars();

    /// <summary>
    ///     Returns the extension of the specified path string.
    /// </summary>
    /// <param name="path">The path string from which to get the extension</param>
    /// <returns>The extension of the specified path (including the period ".")</returns>
    string GetExtension(string path);

    /// <summary>
    ///     Returns the file name and extension of the specified path string.
    /// </summary>
    /// <param name="path">The path string from which to obtain the file name and extension</param>
    /// <returns>The characters after the last directory separator character in path</returns>
    string GetFilename(string path);

    /// <summary>
    ///     Returns the file name of the specified path string without the extension.
    /// </summary>
    /// <param name="path">The path of the file</param>
    /// <returns>The string returned by GetFilename, minus the last period (.) and all characters following it</returns>
    string GetFilenameWithoutExtension(string path);

    /// <summary>
    ///     Combines an array of strings into a path.
    /// </summary>
    /// <param name="paths">An array of parts of the path</param>
    /// <returns>The combined paths</returns>
    string Combine(params string[] paths);
}
