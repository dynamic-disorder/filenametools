using NSubstitute;

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
        _fileSystem = Substitute.For<IFileSystem>();
        _fileSystem.DirectoryExists(TestFolder).Returns(true);
        _sanitizer = Substitute.For<ISanitizer>();
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
        _fileSystem.GetFiles(TestFolder).Returns(new[] { filePathSource });
        _fileSystem.FileExists(filePathDestination).Returns(false);
        _sanitizer.SanitizeFilename(originalName).Returns(expectedName);
        _sanitizer.IsFilenameSanitized(originalName).Returns(false);
        return (filePathSource, filePathDestination);
    }

    private string GivenFileAlreadySanitized(string filename)
    {
        var filePath = Path.Combine(TestFolder, filename);
        _fileSystem.GetFiles(TestFolder).Returns(new[] { filePath });
        _sanitizer.IsFilenameSanitized(filename).Returns(true);
        _sanitizer.SanitizeFilename(filename).Returns(filename);
        return filePath;
    }

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
        _fileSystem.Received(1).MoveFile(filePathSource, filePathDestination);
    }

    [Theory]
    [InlineData("prefix-test.txt", "test.txt")]
    [InlineData("test_old.txt", "test.txt")]
    [InlineData("test.bak.txt", "test.txt")]
    public void RenameFilesRemovingPatterns_ShouldRemoveSpecifiedPatterns(string originalName, string expectedName)
    {
        // Given: a single source file exists and the patterns list contains 'prefix-', '_old', '.bak'
        var (filePathSource, filePathDestination) = GivenSingleFile(originalName, expectedName);
        _fileSystem.FileExists(filePathDestination).Returns(false);
        var patterns = string.Join(Environment.NewLine, "prefix-", "_old", ".bak");
        var sut = SetUpSut();

        // When: RenameFilesRemovingPatterns is executed with the patterns
        sut.RenameFilesRemovingPatterns(patterns);

        // Then: no errors are logged and the file is moved to the expected destination
        sut.Logger.HasErrors.ShouldBeFalse();
        _fileSystem.Received(1).MoveFile(
            Arg.Is<string>(x => x == filePathSource),
            Arg.Is<string>(x => x == filePathDestination));
    }

    [Fact]
    public void RenameFilesToMeetOsRequirements_WhenFolderDoesNotExist_ShouldLogError()
    {
        // Given: a non-existent folder and the file system reports DirectoryExists=false
        var nonExistentFolder = Path.Combine(TestFolder, "NonExistent");
        _fileSystem.DirectoryExists(nonExistentFolder).Returns(false);
        var sut = new FilenameNormalizer(nonExistentFolder, _sanitizer, _fileSystem);

        // When: attempting to rename files in the non-existent folder
        sut.RenameFilesToMeetOsRequirements();

        // Then: an error is logged indicating the folder does not exist
        sut.Logger.HasErrors.ShouldBeTrue();
        sut.Logger.Errors.ShouldContain(e => e.Contains("does not exist"));
    }

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
        _fileSystem.DidNotReceive().MoveFile(Arg.Any<string>(), Arg.Any<string>());
    }
}