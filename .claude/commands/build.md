---
description: Build the FilenameTools solution and all projects
---

## Build Status

!`dotnet build FilenameTools.sln`

## Test Status

!`dotnet test FilenameTools.sln`

## Project Structure

The solution contains these projects:
- FilenameSanitizer (library for filename normalization, net10.0)
- FilenameSanitizerCli (console application for batch processing, net10.0)
- FilenameSanitizer.Tests (unit tests, 78 tests)

The library directory is `FilenameSanitizer/` (lowercase `n`) — on Linux a
`ProjectReference` with any other casing is silently skipped during restore.

## Key Features
- Cross-platform filename compatibility
- Configurable replacement patterns via `sanitizer_replace_patterns.txt`
- Customizable settings via `sanitizer_settings.json`
- Batch directory processing with logging

## Usage Examples
- `FilenameSanitizerCli .` - Sanitize current directory
- `FilenameSanitizerCli C:\Files prefix-;_old` - Remove specific patterns (semicolon-separated on the CLI)
- Check the `FilenameSanitizer.log.<timestamp>.txt` file for the audit trail

## Next Steps
After building, test the CLI with: `dotnet run --project FilenameSanitizerCli -- .`

Tests use the installed .NET 10 runtime — no roll-forward needed.