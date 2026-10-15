using NSubstitute;

using Shouldly;

using Xunit;

namespace FilenameSanitizer.Tests;

/// <summary>
/// Unit tests for the SanitizerSettingsLoader class, covering scenarios for loading settings from files,
/// </summary>
public class SanitizerSettingsLoaderTests
{
    private static readonly string[] s_expectedExcludedChars = ["-", "@"];
    [Fact]
    public void LoadFromFile_ReturnsDefaultSettings_WhenFileDoesNotExist()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        var logger = Substitute.For<ILogger>();
        fileSystem.FileExists(Arg.Any<string>()).Returns(false);
        var loader = new SanitizerSettingsLoader(fileSystem, logger);

        var result = loader.LoadFromFile("nonexistent.json");

        result.ShouldNotBeNull();
        result.ReplacementCharacter.ShouldBe(SanitizerSetting.DefaultCharacter);
        result.ExcludedCharacters.ShouldBeEmpty();
    }

    [Fact]
    public void LoadFromFile_ReturnsDeserializedSettings_WhenFileExistsAndIsValid()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        var logger = Substitute.For<ILogger>();
        fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        var json = """{"ReplacementCharacter": "#", "ExcludedCharacters": ["-", "@"]}""";
        fileSystem.ReadAllText(Arg.Any<string>()).Returns(json);
        var loader = new SanitizerSettingsLoader(fileSystem, logger);

        // Given: a settings file exists with ReplacementCharacter '#' and ExcludedCharacters ['-','@']
        // When: loading the settings file
        var result = loader.LoadFromFile("test_file_name_does_not_matter_what_is.json");

        // Then: the loader returns the deserialized settings
        result.ShouldNotBeNull();
        result.ReplacementCharacter.ShouldBe("#");
        result.ExcludedCharacters.ShouldBe(s_expectedExcludedChars);
    }

    [Fact]
    public void LoadFromFile_LogsErrorAndReturnsDefault_WhenDeserializationFails()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        var logger = Substitute.For<ILogger>();
        fileSystem.FileExists(Arg.Any<string>()).Returns(true);
        fileSystem.ReadAllText(Arg.Any<string>()).Returns("not valid json");
        var loader = new SanitizerSettingsLoader(fileSystem, logger);

        var result = loader.LoadFromFile("settings.json");

        result.ShouldNotBeNull();
        result.ReplacementCharacter.ShouldBe(SanitizerSetting.DefaultCharacter);
        result.ExcludedCharacters.ShouldBeEmpty();
        logger.Received().LogError(Arg.Any<string>(), Arg.Any<Exception>());
    }

    [Fact]
    public void LoadFromFile_UsesProvidedBaseDirectory()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        var logger = Substitute.For<ILogger>();
        fileSystem.FileExists(Arg.Any<string>()).Returns(false);
        var customBaseDir = "/custom/base/dir";
        var loader = new SanitizerSettingsLoader(fileSystem, logger, customBaseDir);

        // When: loading settings with a provided base directory
        loader.LoadFromFile("settings.json");

        // Then: the loader checks for the file in the provided base directory
        fileSystem.Received().FileExists(Path.Combine(customBaseDir, "settings.json"));
    }
}