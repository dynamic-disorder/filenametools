using System.Collections.Generic;

using FakeItEasy;

using Shouldly;

using Xunit;

namespace FilenameSanitizer.Tests;

/// <summary>
/// Covers loading replacement patterns, applying them to filenames, and filtering invalid characters.
/// </summary>
public class PatternLoaderTests
{
    private static readonly string[] s_testPatternLines = ["bad", "big", "wolf", "_", "# comment", ""];
    private static readonly char[] s_testInvalidChars = ['!', '?', '-', '@'];

    /// <summary>
    /// Verifies that the patterns file is read and its comment and blank lines are ignored.
    /// </summary>
    [Fact]
    public void PatternLoader_LoadReplacementPatterns_WhenFileExists_ShouldLoadPatterns()
    {
        // Given: a mocked file system with patterns
        var fileSystem = A.Fake<IFileSystem>();
        A.CallTo(() => fileSystem.Combine(A<string[]>.Ignored)).Returns("test_patterns.txt");
        A.CallTo(() => fileSystem.FileExists("test_patterns.txt")).Returns(true);
        A.CallTo(() => fileSystem.ReadAllLines("test_patterns.txt")).Returns(s_testPatternLines);
        
        var patternLoader = new PatternLoader(fileSystem);
        
        // When: loading patterns
        var patterns = patternLoader.LoadReplacementPatterns();
        
        // Then: patterns should be loaded, ignoring comments and empty lines
        patterns.ShouldNotBeNull();
        patterns.ShouldBe(new List<string> { "bad", "big", "wolf", "_" });
    }

    /// <summary>
    /// Verifies that a missing patterns file yields an empty list rather than a failure.
    /// </summary>
    [Fact]
    public void PatternLoader_LoadReplacementPatterns_WhenFileDoesNotExist_ShouldReturnEmptyList()
    {
        // Given: a mocked file system without patterns file
        var fileSystem = A.Fake<IFileSystem>();
        A.CallTo(() => fileSystem.Combine(A<string[]>.Ignored)).Returns("test_patterns.txt");
        A.CallTo(() => fileSystem.FileExists("test_patterns.txt")).Returns(false);
        
        var patternLoader = new PatternLoader(fileSystem);
        
        // When: loading patterns
        var patterns = patternLoader.LoadReplacementPatterns();
        
        // Then: patterns should be empty list
        patterns.ShouldNotBeNull();
        patterns.ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies that every occurrence of each pattern is replaced with the replacement text.
    /// </summary>
    [Fact]
    public void PatternLoader_ApplyReplacementPatterns_ShouldReplaceAllPatterns()
    {
        // Given: patterns and replacement character
        var patterns = new List<string> { "bad", "big", "wolf", "_" };
        var replacement = " ";
        var patternLoader = new PatternLoader(A.Fake<IFileSystem>());
        
        // When: applying patterns
        var result = patternLoader.ApplyReplacementPatterns("test_bad_big_wolf.txt", patterns, replacement);
        
        // Then: all patterns should be replaced with space
        result.ShouldBe("test      .txt");
    }

    /// <summary>
    /// Verifies that an empty pattern list leaves the filename unchanged.
    /// </summary>
    [Fact]
    public void PatternLoader_ApplyReplacementPatterns_WithEmptyPatterns_ShouldReturnOriginal()
    {
        // Given: empty patterns list
        var patterns = new List<string>();
        var replacement = " ";
        var patternLoader = new PatternLoader(A.Fake<IFileSystem>());
        
        // When: applying empty patterns
        var result = patternLoader.ApplyReplacementPatterns("test_file.txt", patterns, replacement);
        
        // Then: original string should be returned unchanged
        result.ShouldBe("test_file.txt");
    }

    /// <summary>
    /// Verifies that applying patterns leaves characters outside the pattern list untouched, dash included.
    /// </summary>
    [Fact]
    public void PatternLoader_ApplyReplacementPatterns_WithDashExcluded_ShouldPreserveDash()
    {
        // Given: patterns that include characters that might be excluded elsewhere
        var patterns = new List<string> { "bad", "big" };
        var replacement = " ";
        var patternLoader = new PatternLoader(A.Fake<IFileSystem>());
        
        // When: applying patterns to a string containing dash (which is excluded in settings)
        var input = "test-bad-big.txt";
        var result = patternLoader.ApplyReplacementPatterns(input, patterns, replacement);
        
        // Then: patterns should be replaced, dash preserved
        result.ShouldBe("test- - .txt");
    }

    /// <summary>
    /// Verifies that an excluded character is removed from the invalid-character set.
    /// </summary>
    [Fact]
    public void PatternLoader_GetInvalidCharacters_WhenDashExcluded_ShouldNotIncludeDash()
    {
        // Given: settings with dash excluded
        var settings = A.Fake<ISanitizerSetting>();
        A.CallTo(() => settings.ReplacementCharacter).Returns("_");
        A.CallTo(() => settings.ExcludedCharacters).Returns(new List<string> { "-" });
        
        var fileSystem = A.Fake<IFileSystem>();
        A.CallTo(() => fileSystem.GetInvalidFilenameChars()).Returns(s_testInvalidChars);
        
        var patternLoader = new PatternLoader(fileSystem);
        
        // When: getting invalid characters
        var invalidChars = patternLoader.GetInvalidCharacters(settings);
        
        // Then: dash should not be in the array
        invalidChars.ShouldNotContain('-');
        invalidChars.ShouldContain('!');
        invalidChars.ShouldContain('?');
        invalidChars.ShouldContain('@');
    }

    /// <summary>
    /// Verifies that the replacement character itself is never treated as invalid.
    /// </summary>
    [Fact]
    public void PatternLoader_GetInvalidCharacters_WhenReplacementCharacterIsDash_ShouldNotIncludeDash()
    {
        // Given: settings with dash as replacement character
        var settings = A.Fake<ISanitizerSetting>();
        A.CallTo(() => settings.ReplacementCharacter).Returns("-");
        A.CallTo(() => settings.ExcludedCharacters).Returns(new List<string>());
        
        var fileSystem = A.Fake<IFileSystem>();
        A.CallTo(() => fileSystem.GetInvalidFilenameChars()).Returns(s_testInvalidChars);
        
        var patternLoader = new PatternLoader(fileSystem);
        
        // When: getting invalid characters
        var invalidChars = patternLoader.GetInvalidCharacters(settings);
        
        // Then: dash should not be in the array (it's the replacement character)
        invalidChars.ShouldNotContain('-');
        invalidChars.ShouldContain('!');
        invalidChars.ShouldContain('?');
        invalidChars.ShouldContain('@');
    }

    /// <summary>
    /// Verifies that every invalid character is returned when nothing is excluded.
    /// </summary>
    [Fact]
    public void PatternLoader_GetInvalidCharacters_WithNoExclusions_ShouldIncludeAllInvalidChars()
    {
        // Given: settings with no excluded characters and different replacement character
        var settings = A.Fake<ISanitizerSetting>();
        A.CallTo(() => settings.ReplacementCharacter).Returns("_");
        A.CallTo(() => settings.ExcludedCharacters).Returns(new List<string>());
        
        var fileSystem = A.Fake<IFileSystem>();
        A.CallTo(() => fileSystem.GetInvalidFilenameChars()).Returns(s_testInvalidChars);
        
        var patternLoader = new PatternLoader(fileSystem);
        
        // When: getting invalid characters
        var invalidChars = patternLoader.GetInvalidCharacters(settings);
        
        // Then: all invalid chars should be included
        invalidChars.ShouldContain('!');
        invalidChars.ShouldContain('?');
        invalidChars.ShouldContain('-');
        invalidChars.ShouldContain('@');
    }

    /// <summary>
    /// Verifies that the replacement character is not flagged for replacement.
    /// </summary>
    [Fact]
    public void PatternLoader_ShouldIncludeCharacter_WhenCharacterIsReplacement_ShouldReturnFalse()
    {
        // Given: settings with underscore as replacement character
        var settings = A.Fake<ISanitizerSetting>();
        A.CallTo(() => settings.ReplacementCharacter).Returns("_");
        A.CallTo(() => settings.ExcludedCharacters).Returns(new List<string>());
        
        var fileSystem = A.Fake<IFileSystem>();
        var patternLoader = new PatternLoader(fileSystem);
        
        // When: checking if underscore should be included
        var shouldInclude = patternLoader.ShouldIncludeCharacter('_', settings);
        
        // Then: should return false (replacement character should not be replaced)
        shouldInclude.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that an excluded character is not flagged for replacement.
    /// </summary>
    [Fact]
    public void PatternLoader_ShouldIncludeCharacter_WhenCharacterIsExcluded_ShouldReturnFalse()
    {
        // Given: settings with dash excluded
        var settings = A.Fake<ISanitizerSetting>();
        A.CallTo(() => settings.ReplacementCharacter).Returns("_");
        A.CallTo(() => settings.ExcludedCharacters).Returns(new List<string> { "-" });
        
        var fileSystem = A.Fake<IFileSystem>();
        var patternLoader = new PatternLoader(fileSystem);
        
        // When: checking if dash should be included
        var shouldInclude = patternLoader.ShouldIncludeCharacter('-', settings);
        
        // Then: should return false (excluded character should not be replaced)
        shouldInclude.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that an invalid character outside the exclusions is flagged for replacement.
    /// </summary>
    [Fact]
    public void PatternLoader_ShouldIncludeCharacter_WhenCharacterIsValid_ShouldReturnTrue()
    {
        // Given: settings with no exclusions for the character
        var settings = A.Fake<ISanitizerSetting>();
        A.CallTo(() => settings.ReplacementCharacter).Returns("_");
        A.CallTo(() => settings.ExcludedCharacters).Returns(new List<string>());
        
        var fileSystem = A.Fake<IFileSystem>();
        var patternLoader = new PatternLoader(fileSystem);
        
        // When: checking if an invalid character should be included
        var shouldInclude = patternLoader.ShouldIncludeCharacter('@', settings);
        
        // Then: should return true (invalid character should be replaced)
        shouldInclude.ShouldBeTrue();
    }
}
