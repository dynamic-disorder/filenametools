namespace FilenameSanitizer;

/// <summary>
///    A simple implementation of IFileSystem that interacts with the actual file system.
/// </summary>
public class FileSystem : IFileSystem
{
    /// <summary>
    ///   Checks if a directory exists at the specified path.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <returns>True if the directory exists; otherwise, false.</returns>
    public bool DirectoryExists(string path) => Directory.Exists(path);

    /// <summary>
    ///  Checks if a file exists at the specified path.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>True if the file exists; otherwise, false.</returns>
    public bool FileExists(string path) => File.Exists(path);

    /// <summary>
    ///   Gets the files in the specified directory.
    /// </summary>
    /// <param name="path">The path to the directory.</param>
    /// <returns>An array of file names.</returns>
    public string[] GetFiles(string path) => Directory.GetFiles(path);

    /// <summary>
    ///   Moves a file from the source path to the destination path.
    /// </summary>
    /// <param name="sourceFilename">The path to the source file.</param>
    /// <param name="destFilename">The path to the destination file.</param>
    public void MoveFile(string sourceFilename, string destFilename) => File.Move(sourceFilename, destFilename);
 
    /// <summary>
    ///   Reads all text from the specified file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>The contents of the file as a string.</returns>
    public string ReadAllText(string path) => File.ReadAllText(path);

    /// <summary>
    ///     Opens a text file, reads all lines of the file, and then closes the file.
    /// </summary>
    /// <param name="path">The file to read</param>
    /// <returns>An array of strings containing all lines of the file</returns>
    public string[] ReadAllLines(string path) => File.ReadAllLines(path);

    /// <summary>
    ///     Gets the invalid characters for file names in the current operating system.
    /// </summary>
    /// <returns>An array containing the characters that are not allowed in file names</returns>
    public char[] GetInvalidFilenameChars() => Path.GetInvalidFileNameChars();

    /// <summary>
    ///     Returns the extension of the specified path string.
    /// </summary>
    /// <param name="path">The path string from which to get the extension</param>
    /// <returns>The extension of the specified path (including the period ".")</returns>
    public string GetExtension(string path) => Path.GetExtension(path);

    /// <summary>
    ///     Returns the file name and extension of the specified path string.
    /// </summary>
    /// <param name="path">The path string from which to obtain the file name and extension</param>
    /// <returns>The characters after the last directory separator character in path</returns>
    public string GetFilename(string path) => Path.GetFileName(path);

    /// <summary>
    ///     Returns the file name of the specified path string without the extension.
    /// </summary>
    /// <param name="path">The path of the file</param>
    /// <returns>The string returned by GetFilename, minus the last period (.) and all characters following it</returns>
    public string GetFilenameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);

    /// <summary>
    ///     Combines an array of strings into a path.
    /// </summary>
    /// <param name="paths">An array of parts of the path</param>
    /// <returns>The combined paths</returns>
    public string Combine(params string[] paths) => Path.Combine(paths);
}
