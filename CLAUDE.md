# FilenameTools projects

.NET C# tools for sanitizing filenames to ensure OS compatibility. Contains libraries and console apps for batch filename normalization.

**Core Projects:**
- **FilenameSanitizer** (`FilenameSanitizer/`) — Library for filename normalization and sanitization (net10.0)
- **FilenameSanitizerCli** (`FilenameSanitizerCli/`) — Console application for batch filename processing (net10.0)
- **FilenameSanitizer.Tests** (`FilenameSanitizer.Tests/`) — Unit tests for the library (xUnit, 78 tests)

> **Casing matters.** The library directory is `FilenameSanitizer/` (lowercase `n`). It was renamed from `FileNameSanitizer/`, and the tests project still pointed at the old casing — on Linux that reference is silently *skipped* during restore and only fails later at compile time with a missing-assembly error. Keep every `ProjectReference` byte-for-byte identical to the on-disk name.

**Key Features:**
- Removes invalid characters from filenames for cross-platform compatibility
- Configurable replacement patterns via `sanitizer_replace_patterns.txt`
- Customizable settings via `sanitizer_settings.json`
- Batch processing of entire directories
- Pattern-based filename cleaning — semicolon-separated on the CLI, new-line separated in the library API
- Comprehensive logging with operation logs

**Usage:** `FilenameSanitizerCli <folder> [patterns]` — Sanitize filenames in specified folder, optionally removing patterns.

```bash
FilenameSanitizerCli .                          # sanitize the current directory
FilenameSanitizerCli /home/me/Files             # sanitize a folder
FilenameSanitizerCli /home/me/Files 'prefix-;_old;.bak'   # also strip these patterns
```

**Purpose:** Ensures filenames are compatible across Windows, Linux, and macOS by removing/resolving problematic characters and patterns.

## Cross-platform behaviour

- **Windows reserved names** (`CON`, `PRN`, `AUX`, `NUL`, `COM1-4`, `LPT1-4`) are prefixed with `_` on **every** platform, not just Windows, so `PRN.doc` becomes `_PRN.doc` everywhere. This keeps a folder portable if it is later synced to Windows.
- **Invalid characters** come from `Path.GetInvalidFileNameChars()` (OS-dependent) *plus* a fixed POSIX-unsafe list in `PatternLoader`, so the output is a superset that is safe on all three platforms.
- **`-c Release` builds against the sibling project.** An earlier Release-only `PackageReference FilenameSanitizer 1.0.0` did not exist on NuGet and made `-c Release` fail with `NU1101`; the library still produces its `.nupkg` when built.

## Configuration Files

- `sanitizer_replace_patterns.txt` — replacement patterns, one per line
- `sanitizer_settings.json` — runtime configuration (`ReplacementCharacter`, `ExcludedCharacters`)
- `FilenameSanitizer.log.<timestamp>.txt` — auto-generated audit trail, written next to the processed folder (git-ignored)

## Common Tasks

```bash
# From the repository root
dotnet build FilenameTools.sln              # Debug; also works with -c Release
dotnet test FilenameTools.sln               # 78 tests
dotnet run --project FilenameSanitizerCli -- <folder> [patterns]
```

Every project targets `net10.0` and the .NET 10 runtime is installed, so `dotnet test`/`dotnet run`
work directly — no roll-forward or extra runtime install required.

See `.claude/commands/build.md` for the automated build/test command. The
cross-platform findings that matter here are the casing rule above.

## Known Issues

- **Pattern mode skips normal filenames.** `FilenameNormalizer.RenameFileRemovingPatternsIfNeeded` calls `ShouldRenameFile`, which gates on `IsFilenameSanitized(originalName)` — true for any already-clean name. So `prefix-test.txt` logs *"already sanitized"* instead of becoming `test.txt`; only files that *also* contain invalid characters get their patterns stripped. Fix is to compare the pattern-stripped name with the original instead of consulting `IsFilenameSanitized`.
- **All projects target `net10.0`** (the CLI, the library, and the tests), so the whole solution builds with the .NET 10 SDK.
- **CLI interaction:** the tool prints the log-file path and waits for a keypress, but only when stdin is a TTY (`Console.IsInputRedirected`). Scripted/piped runs exit cleanly instead of throwing.
