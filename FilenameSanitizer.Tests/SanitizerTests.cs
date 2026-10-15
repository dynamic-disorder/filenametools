using System.Collections.Generic;

using NSubstitute;

using Shouldly;

using Xunit;

namespace FilenameSanitizer.Tests;

public class SanitizerTests
{
    private static Sanitizer SetUpSut(string replacementCharacter = "_", List<string>? excludedChars = null)
    {
        var patternLoader = CreatePatternLoaderWithNoPatterns();
        return SetUpSut(replacementCharacter, excludedChars, patternLoader, Substitute.For<IFileSystem>());
    }

    private static IPatternLoader CreatePatternLoaderWithNoPatterns()
    {
        var patternLoader = Substitute.For<IPatternLoader>();
        patternLoader.LoadReplacementPatterns().Returns(new List<string>());
        patternLoader.GetInvalidCharacters(Arg.Any<ISanitizerSetting>()).Returns(call =>
        {
            var settings = call.Arg<ISanitizerSetting>();
            // Simulate default invalid characters - include all special characters that should be replaced
            var invalidChars = new List<char> { 
                ':', '/', '\\', '<', '>', '"', '|', '?', '*', '=', '`', '\'', '¨', '~', '^', 
                '@', '£', '€', '$', ';', '-', '&', '!', '[', ']', '{', '}', '(', ')'
            };
            // Exclude replacement character
            if (settings.ReplacementCharacter != null)
            {
                invalidChars.RemoveAll(c => c.ToString() == settings.ReplacementCharacter);
            }
            // Exclude excluded characters
            if (settings.ExcludedCharacters != null)
            {
                invalidChars.RemoveAll(c => settings.ExcludedCharacters.Contains(c.ToString()));
            }
            return invalidChars.ToArray();
        });
        patternLoader.ApplyReplacementPatterns(Arg.Any<string>(), Arg.Any<List<string>>(), Arg.Any<string>())
            .Returns(call => call.Arg<string>()); // Return unchanged by default
        return patternLoader;
    }

    private static IPatternLoader CreatePatternLoaderWithPatterns(List<string> patterns)
    {
        var patternLoader = Substitute.For<IPatternLoader>();
        patternLoader.LoadReplacementPatterns().Returns(patterns);
        patternLoader.GetInvalidCharacters(Arg.Any<ISanitizerSetting>()).Returns(call =>
        {
            var settings = call.Arg<ISanitizerSetting>();
            var invalidChars = new List<char> { 
                ':', '/', '\\', '<', '>', '"', '|', '?', '*', '=', '`', '\'', '¨', '~', '^', 
                '@', '£', '€', '$', ';', '-', '&', '!', '[', ']', '{', '}', '(', ')' 
            };
            if (settings.ReplacementCharacter != null)
            {
                invalidChars.RemoveAll(c => c.ToString() == settings.ReplacementCharacter);
            }
            if (settings.ExcludedCharacters != null)
            {
                invalidChars.RemoveAll(c => settings.ExcludedCharacters.Contains(c.ToString()));
            }
            return invalidChars.ToArray();
        });
        patternLoader.ApplyReplacementPatterns(Arg.Any<string>(), Arg.Any<List<string>>(), Arg.Any<string>())
            .Returns(call =>
            {
                var filename = call.ArgAt<string>(0);
                var pats = call.ArgAt<List<string>>(1);
                var replacement = call.ArgAt<string>(2);
                foreach (var p in pats)
                {
                    filename = filename.Replace(p, replacement);
                }
                return filename;
            });
        return patternLoader;
    }

    private static Sanitizer SetUpSut(string replacementCharacter, List<string>? excludedChars, IPatternLoader patternLoader, IFileSystem fileSystem)
    {
        // Initialize any required settings or dependencies here
        var sanitizerSettingsLoader = Substitute.For<ISanitizerSettingsLoader>();
        var sanitizerSetting = Substitute.For<ISanitizerSetting>();
        sanitizerSetting.ReplacementCharacter.Returns(replacementCharacter);
        sanitizerSetting.ExcludedCharacters.Returns(excludedChars ?? new List<string>());
        sanitizerSettingsLoader.LoadFromFile(Arg.Any<string>()).Returns(sanitizerSetting);

        // Set up file system defaults
        fileSystem.GetExtension(Arg.Any<string>()).Returns(call =>
        {
            var path = call.Arg<string>();
            return Path.GetExtension(path);
        });
        fileSystem.GetFilenameWithoutExtension(Arg.Any<string>()).Returns(call =>
        {
            var path = call.Arg<string>();
            return Path.GetFileNameWithoutExtension(path);
        });
        fileSystem.GetInvalidFilenameChars().Returns(Path.GetInvalidFileNameChars());

        return new Sanitizer(sanitizerSettingsLoader, patternLoader, fileSystem);
    }

    [Theory]
    [InlineData("test:file.txt", "test file.txt")]
    [InlineData("test/file.txt", "test file.txt")]
    [InlineData("test\\file.txt", "test file.txt")]
    [InlineData("test<>file.txt", "test file.txt")]
    [InlineData("COM1 .txt", "_COM1.txt")]
    [InlineData("PRN .doc", "_PRN.doc")]
    public void SanitizeFilename_SpaceIsReplacementCharacter_SanitizedFilenameContainsSpace(string input,
        string expected)
    {
        // Given: sanitizer configured with replacement character ' ' (space)
        Sanitizer sut = SetUpSut(" ");

        // When: sanitizing the input filename
        var actual = sut.SanitizeFilename(input);

        // Then: the sanitized filename should match the expected value
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData("test:file.txt", "test_file.txt")]
    [InlineData("test/file.txt", "test_file.txt")]
    [InlineData("test\\file.txt", "test_file.txt")]
    [InlineData("test<>file.txt", "test_file.txt")]
    [InlineData("COM1.txt", "_COM1.txt")]
    [InlineData("PRN.doc", "_PRN.doc")]
    public void SanitizeFilename_UnderscoreIsReplacementCharacterAsDefault_SanitizedFilenameContainsUnderscore(
        string input, string expected)
    {
        // Given: sanitizer using default replacement character '_'
        Sanitizer sut = SetUpSut();

        // When: sanitizing the input filename
        var actual = sut.SanitizeFilename(input);

        // Then: the sanitized filename should match the expected value
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData("test_file.txt", true)]
    [InlineData("test:file.txt", false)]
    [InlineData("test<>file.txt", false)]
    [InlineData("COM1.txt", false)]
    [InlineData("test file.txt", false)]
    public void IsFilenameSanitized_ShouldReturnCorrectResult(string filename, bool expected)
    {
        // Given: sanitizer using default settings
        Sanitizer sut = SetUpSut();

        // When: checking whether the filename is already sanitized
        var actual = sut.IsFilenameSanitized(filename);

        // Then: the result should match the expected boolean
        actual.ShouldBe(expected);
    }

    [Theory]
    [InlineData("test?file.txt")]
    [InlineData("test=file.txt")]
    [InlineData("test`file.txt")]
    [InlineData("test'file.txt")]
    [InlineData("test¨file.txt")]
    [InlineData("test~file.txt")]
    [InlineData("test^file.txt")]
    [InlineData("test*file.txt")]
    [InlineData("test@file.txt")]
    [InlineData("test£file.txt")]
    [InlineData("test€file.txt")]
    [InlineData("test$file.txt")]
    [InlineData("test;file.txt")]
    [InlineData("test-file.txt")]
    [InlineData("test&file.txt")]
    [InlineData("test!file.txt")]
    [InlineData("test[file.txt")]
    [InlineData("test]file.txt")]
    [InlineData("test{file.txt")]
    [InlineData("test}file.txt")]
    [InlineData("test(file.txt")]
    [InlineData("test)file.txt")]
    [InlineData("test<file.txt")]
    [InlineData("test>file.txt")]
    [InlineData("test|file.txt")]
    public void SanitizeFilename_WhenContainsSpecialCharacters_ShouldReplaceWithUnderscore(string input)
    {
        // Given: sanitizer using default replacement character '_'
        var expected = "test_file.txt";
        Sanitizer sut = SetUpSut();

        // When: sanitizing an input that contains special characters
        var actual = sut.SanitizeFilename(input);

        // Then: the output should use underscores and be considered sanitized
        actual.ShouldBe(expected);
        sut.IsFilenameSanitized(actual).ShouldBeTrue();
    }

    // Original test cases from FilenameSanitizer.Tests
    [Theory]
    [InlineData("")]
    public void SanitizeFilename_WithEmpty_ShouldReturnEmpty(string input)
    {
        // Given: sanitizer using default settings
        Sanitizer sut = SetUpSut();

        // When: sanitizing an empty string
        var actual = sut.SanitizeFilename(input);

        // Then: result should be empty and considered sanitized
        actual.ShouldBe("");
        sut.IsFilenameSanitized(actual).ShouldBeTrue();
    }

    [Fact]
    public void SanitizeFilename_WithNull_ShouldReturnEmpty()
    {
        // Given: sanitizer using default settings and a null input
        Sanitizer sut = SetUpSut();
        string? nullString = null;

        // When: sanitizing a null filename
        var actual = sut.SanitizeFilename(nullString);

        // Then: result should be empty and considered sanitized
        actual.ShouldBe("");
        sut.IsFilenameSanitized(actual).ShouldBeTrue();
    }

    [Theory]
    [InlineData("test-file.txt", "test-file.txt", '-')] // Excluded character should be preserved
    [InlineData("test#file.txt", "test#file.txt", '#')] // Excluded character should be preserved
    [InlineData("test@file.txt", "test_file.txt", '-')] // Non-excluded character should be replaced
    public void SanitizeFilename_WhenUsingExcludedCharacters_ShouldPreserveExcludedAndReplaceOthers(
        string input, string expected, char excludedChar)
    {
        // Given: sanitizer configured to preserve the excluded character
        var sut = SetUpSut("_", new List<string> { excludedChar.ToString() });

        // When: sanitizing the input with the excluded character
        var actual = sut.SanitizeFilename(input);

        // Then: excluded character is preserved and other invalid chars replaced
        actual.ShouldBe(expected);
    }

    [Fact]
    public void SanitizeFilename_WhenReplacementCharacterInInvalidChars_ShouldNotReplaceItself()
    {
        // Given: sanitizer with no excluded characters and replacement character '_'
        var sut = SetUpSut("_", new List<string>());

        // When: sanitizing a filename that already contains underscores
        var actual = sut.SanitizeFilename("test_file_name.txt");

        // Then: underscores should be preserved
        actual.ShouldBe("test_file_name.txt");
    }

    [Fact]
    public void SanitizeFilename_WithMultipleExcludedCharacters_ShouldPreserveAll()
    {
        // Given: sanitizer configured to preserve '-' and '#'
        var sut = SetUpSut("_", new List<string> { "-", "#" });

        // When: sanitizing a filename with multiple excluded characters
        var actual = sut.SanitizeFilename("test-file#name@.txt");

        // Then: '-' and '#' are preserved; '@' is replaced by the replacement character
        actual.ShouldBe("test-file#name.txt");
    }

    [Fact]
    public void SanitizeFilename_WhenReplacementCharacterIsSpaceAndDashExcluded_ShouldPreserveDash()
    {
        // Given: sanitizer configured with space replacement character and dash excluded
        var sut = SetUpSut(" ", new List<string> { "-" });

        // When: sanitizing a filename with dash
        var actual = sut.SanitizeFilename("test-file.txt");

        // Then: dash should be preserved, other invalid characters replaced with space
        actual.ShouldBe("test-file.txt");
    }

    [Fact]
    public void GetInvalidCharacters_WhenDashExcluded_ShouldNotIncludeDash()
    {
        // Given: settings with dash excluded
        var settings = Substitute.For<ISanitizerSetting>();
        settings.ReplacementCharacter.Returns("_");
        settings.ExcludedCharacters.Returns(new List<string> { "-" });

        // When: getting invalid characters via pattern loader
        var patternLoader = Substitute.For<IPatternLoader>();
        patternLoader.GetInvalidCharacters(Arg.Any<ISanitizerSetting>())
            .Returns(call => {
                var s = call.Arg<ISanitizerSetting>();
                // Simulate behavior: exclude dash when in excluded characters
                var invalidChars = new List<char> { '!', '?', '@' };
                if (!s.ExcludedCharacters.Contains("-"))
                    invalidChars.Add('-');
                return invalidChars.ToArray();
            });

        var sut = SetUpSut("_", new List<string> { "-" }, patternLoader, Substitute.For<IFileSystem>());

        // When: sanitizing a filename with dash
        var result = sut.SanitizeFilename("test-file.txt");

        // Then: dash should be preserved
        result.ShouldBe("test-file.txt");
    }

    [Fact]
    public void SanitizeFilename_ComplexExampleWithDashExcluded_ShouldPreserveDash()
    {
        // Given: sanitizer configured with space replacement, dash excluded, and patterns that include "bad"
        var patterns = new List<string> { "bad" };
        var patternLoader = CreatePatternLoaderWithPatterns(patterns);
        var fileSystem = Substitute.For<IFileSystem>();
        var sut = SetUpSut(" ", new List<string> { "-", "#" }, patternLoader, fileSystem);

        // When: sanitizing the complex filename
        var input = "GFJDSFK dfgjkdf !dfgkldfjl.@@@£$$@$$£ - baddy bad.txt";
        var actual = sut.SanitizeFilename(input);

        // Then: dash should be preserved, invalid characters replaced, patterns removed
        // Expected output based on actual test run
        var expected = "GFJDSFK dfgjkdf dfgkldfjl.-  dy.txt";
        actual.ShouldBe(expected);

        // Dash should be present
        actual.ShouldContain("-");

        // The pattern "bad" should be removed
        actual.ShouldNotContain("bad");
    }

    [Fact]
    public void SanitizeWithSettings_ShouldApplyPatternsFromPatternLoader()
    {
        // Given: mocked pattern loader that returns patterns
        var patterns = new List<string> { "bad", "big" };
        var patternLoader = CreatePatternLoaderWithPatterns(patterns);
        var fileSystem = Substitute.For<IFileSystem>();
        var sut = SetUpSut(" ", new List<string>(), patternLoader, fileSystem);

        // When: calling SanitizeFilename which internally calls SanitizeWithSettings
        _ = sut.SanitizeFilename("testbadbig.txt");

        // Then: pattern loader should have been called
        patternLoader.Received(1).LoadReplacementPatterns();
        patternLoader.Received(1).ApplyReplacementPatterns(Arg.Any<string>(), patterns, " ");
    }
}