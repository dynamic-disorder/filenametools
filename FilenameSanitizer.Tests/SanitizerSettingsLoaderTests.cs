using FakeItEasy;

using Shouldly;

using Xunit;

namespace FilenameSanitizer.Tests;

/// <summary>
/// Unit tests for the SanitizerSettingsLoader class, covering scenarios for loading settings from files,
/// </summary>
public class SanitizerSettingsLoaderTests
{
    private static readonly string[] s_expectedExcludedChars = ["-", "@"];

    /// <summary>
    /// Verifies that a missing settings file falls back to the default character and no exclusions.
    /// </summary>
    [Fact]
    public void LoadFromFile_ReturnsDefaultSettings_WhenFileDoesNotExist()
    {
        var fileSystem = A.Fake<IFileSystem>();
        var logger = A.Fake<ILogger>();
        A.CallTo(() => fileSystem.FileExists(A<string>.Ignored)).Returns(false);
        var loader = new SanitizerSettingsLoader(fileSystem, logger);

        var result = loader.LoadFromFile("nonexistent.json");

        result.ShouldNotBeNull();
        result.ReplacementCharacter.ShouldBe(SanitizerSetting.DefaultCharacter);
        result.ExcludedCharacters.ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies that a valid settings file is deserialised into the replacement character and exclusions.
    /// </summary>
    [Fact]
    public void LoadFromFile_ReturnsDeserializedSettings_WhenFileExistsAndIsValid()
    {
        var fileSystem = A.Fake<IFileSystem>();
        var logger = A.Fake<ILogger>();
        A.CallTo(() => fileSystem.FileExists(A<string>.Ignored)).Returns(true);
        var json = """{"ReplacementCharacter": "#", "ExcludedCharacters": ["-", "@"]}""";
        A.CallTo(() => fileSystem.ReadAllText(A<string>.Ignored)).Returns(json);
        var loader = new SanitizerSettingsLoader(fileSystem, logger);

        // Given: a settings file exists with ReplacementCharacter '#' and ExcludedCharacters ['-','@']
        // When: loading the settings file
        var result = loader.LoadFromFile("test_file_name_does_not_matter_what_is.json");

        // Then: the loader returns the deserialized settings
        result.ShouldNotBeNull();
        result.ReplacementCharacter.ShouldBe("#");
        result.ExcludedCharacters.ShouldBe(s_expectedExcludedChars);
    }

    /// <summary>
    /// Verifies that malformed settings JSON is logged and falls back to the default settings.
    /// </summary>
    [Fact]
    public void LoadFromFile_LogsErrorAndReturnsDefault_WhenDeserializationFails()
    {
        var fileSystem = A.Fake<IFileSystem>();
        var logger = A.Fake<ILogger>();
        A.CallTo(() => fileSystem.FileExists(A<string>.Ignored)).Returns(true);
        A.CallTo(() => fileSystem.ReadAllText(A<string>.Ignored)).Returns("not valid json");
        var loader = new SanitizerSettingsLoader(fileSystem, logger);

        var result = loader.LoadFromFile("settings.json");

        result.ShouldNotBeNull();
        result.ReplacementCharacter.ShouldBe(SanitizerSetting.DefaultCharacter);
        result.ExcludedCharacters.ShouldBeEmpty();
        A.CallTo(() => logger.LogError(A<string>.Ignored, A<System.Exception>.Ignored)).MustHaveHappened();
    }

    /// <summary>
    /// Verifies that settings are looked up inside the base directory the loader was given.
    /// </summary>
    [Fact]
    public void LoadFromFile_UsesProvidedBaseDirectory()
    {
        var fileSystem = A.Fake<IFileSystem>();
        var logger = A.Fake<ILogger>();
        A.CallTo(() => fileSystem.FileExists(A<string>.Ignored)).Returns(false);
        var customBaseDir = "/custom/base/dir";
        var loader = new SanitizerSettingsLoader(fileSystem, logger, customBaseDir);

        // When: loading settings with a provided base directory
        loader.LoadFromFile("settings.json");

        // Then: the loader checks for the file in the provided base directory
        A.CallTo(() => fileSystem.FileExists(Path.Combine(customBaseDir, "settings.json"))).MustHaveHappened();
    }
}
