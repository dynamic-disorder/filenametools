---
paths:
  - "**/*.cs"
  - "**/*.csproj"
  - "**/*.sln"
  - "**/sanitizer_*.txt"
  - "**/sanitizer_*.json"
---

# Filename Sanitization Rules for FilenameTools

## Project Purpose
- Cross-platform filename compatibility (Windows, Linux, macOS)
- Remove invalid characters from filenames
- Configurable replacement patterns
- Batch processing of directories

## Operating System Considerations
- Windows: Invalid characters: `<>:"/\|?*`
- Linux/macOS: Invalid characters: `/` and null byte
- The sanitizer applies the **union** of both sets, so output is safe everywhere
- Windows reserved names (`CON`, `PRN`, `AUX`, `NUL`, `COM1-4`, `LPT1-4`) get a `_`
  prefix on **every** platform, keeping a folder portable to Windows
- Case sensitivity: Windows is case-insensitive, Linux/macOS are case-sensitive —
  this applies to project/file references too, not just the sanitized names
- Path separators: Windows uses `\`, others use `/`

## Configuration Files
- `sanitizer_replace_patterns.txt`: patterns to remove, **one per line**
  (the CLI takes them semicolon-separated and converts to new-line separated
  before calling `RenameFilesRemovingPatterns`, whose contract is new-line separated)
- `sanitizer_settings.json`: Customizable settings for the sanitizer
- Patterns format: `prefix-` or `_old`

## .NET Development
- Target framework: .NET 10.0 (the CLI, library, and tests all target `net10.0`)
- Build commands: `dotnet build`, `dotnet run`, `dotnet test`
- Console app: `FilenameSanitizerCli`

## Usage Examples
- `FilenameSanitizerCli .` - Sanitize current directory
- `FilenameSanitizerCli C:\Files prefix-;_old` - Remove patterns "prefix-" and "_old"
- Batch processing with logging to `FilenameSanitizer.log.<timestamp>.txt`

## Known Issue
- Pattern mode skips filenames that are already "sanitized": `ShouldRenameFile` gates on
  `IsFilenameSanitized(originalName)`, so a clean name like `prefix-test.txt` is reported as
  "already sanitized" instead of being stripped to `test.txt`.

## File Operations
- Use caution with file renaming operations
- Always test with dry-run or backup first
- Log operations for audit trail
