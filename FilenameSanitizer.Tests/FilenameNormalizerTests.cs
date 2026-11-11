using FakeItEasy;

using Shouldly;

using Xunit;

namespace FilenameSanitizer.Tests;

/// <summary>
///  Unit tests for covering various scenarios for renaming
///  files to meet OS requirements and removing specified patterns from filenames.
/// </summary>
public class FilenameNormalizerTests
{
    // Platform-neutral absolute folder so the suite runs on Windows and POSIX hosts alike.
    private static readonly string TestFolder = Path.Combine(Path.GetTempPath(), "FilenameSanitizerTests");
    private readonly IFileSystem _fileSystem;

    private readonly ISanitizer _sanitizer;

    public FilenameNormalizerTests()
    {
        _fileSystem = A.Fake<IFileSystem>();
        A.CallTo(() => _fileSystem.DirectoryExists(TestFolder)).Returns(true);
        _sanitizer = A.Fake<ISanitizer>();
        SetUpSut();
    }

    private FilenameNormalizer SetUpSut()
    {
        var sut = new FilenameNormalizer(TestFolder, _sanitizer, _fileSystem);
        return sut;
    }

    private (string source, string destination) GivenSingleFile(string originalName, string expectedName)
    {
        var filePathSource = Path.Combine(TestFolder, originalName);
        var filePathDestination = Path.Combine(TestFolder, expectedName);
        A.CallTo(() => _fileSystem.GetFiles(TestFolder)).Returns(new[] { filePathSource });
        A.CallTo(() => _fileSystem.FileExists(filePathDestination)).Returns(false);
        A.CallTo(() => _sanitizer.SanitizeFilename(originalName)).Returns(expectedName);
        A.CallTo(() => _sanitizer.IsFilenameSanitized(originalName)).Returns(false);
        return (filePathSource, filePathDestination);
    }

    private string GivenFileAlreadySanitized(string filename)
    {
        var filePath = Path.Combine(TestFolder, filename);
        A.CallTo(() => _fileSystem.GetFiles(TestFolder)).Returns(new[] { filePath });
        A.CallTo(() => _sanitizer.IsFilenameSanitized(filename)).Returns(true);
        A.CallTo(() => _sanitizer.SanitizeFilename(filename)).Returns(filename);
        return filePath;
    }

    /// <summary>
    /// Verifies that a file whose name is invalid for the OS is moved to its sanitised name.
    /// </summary>
    [Theory]
    [InlineData("test:file.txt", "test_file.txt")]
    [InlineData("test<>file.txt", "test_file.txt")]
    [InlineData("COM1.txt", "_COM1.txt")]
    [InlineData("PRN.doc", "_PRN.doc")]
    public void RenameFilesToMeetOsRequirements_ShouldSanitizeFilenames(string originalName, string expectedName)
    {
        // Given: a single source file exists and the sanitizer will produce the expected destination name
        var (filePathSource, filePathDestination) = GivenSingleFile(originalName, expectedName);
        var sut = SetUpSut();

        // When: RenameFilesToMeetOsRequirements is executed
        sut.RenameFilesToMeetOsRequirements();

        // Then: no errors are logged and the file is moved to the destination
        sut.Logger.HasErrors.ShouldBeFalse();
        A.CallTo(() => _fileSystem.MoveFile(filePathSource, filePathDestination)).MustHaveHappenedOnceExactly();
    }

    /// <summary>
    /// Verifies that the configured patterns are stripped from a filename that is then renamed.
    /// </summary>
    [Theory]
    [InlineData("prefix-test.txt", "test.txt")]
    [InlineData("test_old.txt", "test.txt")]
    [InlineData("test.bak.txt", "test.txt")]
    public void RenameFilesRemovingPatterns_ShouldRemoveSpecifiedPatterns(string originalName, string expectedName)
    {
        // Given: a single source file exists and the patterns list contains 'prefix-', '_old', '.bak'
        var (filePathSource, filePathDestination) = GivenSingleFile(originalName, expectedName);
        A.CallTo(() => _fileSystem.FileExists(filePathDestination)).Returns(false);
        var patterns = string.Join(Environment.NewLine, "prefix-", "_old", ".bak");
        var sut = SetUpSut();

        // When: RenameFilesRemovingPatterns is executed with the patterns
        sut.RenameFilesRemovingPatterns(patterns);

        // Then: no errors are logged and the file is moved to the expected destination
        sut.Logger.HasErrors.ShouldBeFalse();
        A.CallTo(() => _fileSystem.MoveFile(
            A<string>.That.Matches(x => x == filePathSource),
            A<string>.That.Matches(x => x == filePathDestination))).MustHaveHappenedOnceExactly();
    }

    /// <summary>
    /// Verifies that renaming in a folder that does not exist logs an error instead of touching files.
    /// </summary>
    [Fact]
    public void RenameFilesToMeetOsRequirements_WhenFolderDoesNotExist_ShouldLogError()
    {
        // Given: a non-existent folder and the file system reports DirectoryExists=false
        var nonExistentFolder = Path.Combine(TestFolder, "NonExistent");
        A.CallTo(() => _fileSystem.DirectoryExists(nonExistentFolder)).Returns(false);
        var sut = new FilenameNormalizer(nonExistentFolder, _sanitizer, _fileSystem);

        // When: attempting to rename files in the non-existent folder
        sut.RenameFilesToMeetOsRequirements();

        // Then: an error is logged indicating the folder does not exist
        sut.Logger.HasErrors.ShouldBeTrue();
        sut.Logger.Errors.ShouldContain(e => e.Contains("does not exist"));
    }

    /// <summary>
    /// Verifies that an already valid filename is left in place with a warning and no move.
    /// </summary>
    [Theory]
    [InlineData("test.txt")]
    [InlineData("normal file.doc")]
    public void RenameFilesToMeetOsRequirements_WhenFilenameIsAlreadyValid_ShouldSkip(string filename)
    {
        // Given: a file that is already sanitized according to the sanitizer
        var filePath = GivenFileAlreadySanitized(filename);
        var sut = SetUpSut();

        // When: RenameFilesToMeetOsRequirements is executed
        sut.RenameFilesToMeetOsRequirements();

        // Then: a warning is logged and no file move is performed
        sut.Logger.HasWarnings.ShouldBeTrue();
        sut.Logger.Warnings.ShouldContain(w => w.Contains("already sanitized"));
        A.CallTo(() => _fileSystem.MoveFile(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
    }
}
