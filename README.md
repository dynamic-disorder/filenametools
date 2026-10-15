# FilenameTools

A cross-platform .NET 10 utility for sanitizing filenames — removes or replaces problematic characters, trims whitespace, normalizes casing, and replaces special Unicode characters with ASCII equivalents.

## Structure

The solution (`FilenameTools.sln`) contains three projects:

### FilenameSanitizer (Class Library)

Core library providing filename sanitization logic:

- `Sanitizer` — Main sanitizer engine that processes filenames through configurable rules
- `FilenameNormalizer` — Handles Unicode normalization (NFKD) and ASCII fallback
- `PatternLoader` — Loads sanitization patterns and rules from JSON configuration
- `SanitizerSettingsLoader` — Loads and manages sanitizer settings
- `FileSystem` — Abstraction over file I/O operations

### FilenameSanitizer.Tests (xUnit)

Unit tests covering normalizer, pattern loader, settings loader, and the main sanitizer logic.

### FilenameSanitizerCli (Console App)

Command-line entry point that accepts file/directory paths and applies sanitization rules.

## Usage

```bash
# Build
dotnet build FilenameTools.sln          # -c Release also works

# Test (78 tests; uses the installed .NET 10 runtime)
dotnet test FilenameTools.sln

# Run CLI — optional patterns are semicolon-separated
dotnet run --project FilenameSanitizerCli -- <path> [pattern1;pattern2]
dotnet run --project FilenameSanitizerCli -- /home/me/Files 'prefix-;_old;.bak'
```

The library directory is `FilenameSanitizer/` (lowercase `n`) — keep the casing of every
`ProjectReference` identical to it, otherwise restore silently skips the project on
case-sensitive filesystems and the build fails later with a missing assembly.

Windows reserved names (`CON`, `PRN`, `COM1`, …) are prefixed with `_` on every platform,
so the output stays usable if the folder is later copied to Windows.

## Configuration

Sanitization rules are loaded from JSON configuration files. The sanitizer can:
- Replace or remove illegal filename characters
- Trim leading/trailing whitespace and dots
- Normalize Unicode characters to ASCII equivalents
- Collapse multiple consecutive spaces