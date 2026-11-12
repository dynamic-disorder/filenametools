# FilenameTools — Working Instructions

| | |
| :--- | :--- |
| **Need** | A session with no memory of this repository: the three projects, the one casing rule that breaks Linux builds, the sanitisation contract, and the defect that is documented but not yet fixed. |
| **Why** | The library decides what a filename may contain across three platforms. A wrong assumption either corrupts a real folder or silently skips a project at restore and fails at compile time. |
| **What** | Project layout, the casing rule, build/test/run, the cross-platform sanitisation contract, the configuration files, the pattern-mode defect, and the machine-level agent configuration it relies on. Deliberately not repeated: `.editorconfig`, which owns formatting. |
| **DoD** | A session can build and test the solution, change sanitisation behaviour without breaking portability, and say where the gate and the skills come from. |
| **Top risk** | Renaming or re-casing a project directory without updating every `ProjectReference`, which builds on Windows and fails on Linux — the primary platform. |

Loaded automatically as workspace instructions for anything under `filenametools/`.

## What this repository is

One solution, `FilenameTools.sln`, holding three `net10.0` projects:

| Project | Kind | Role |
| :--- | :--- | :--- |
| `FilenameSanitizer/` | Class library | The engine: `Sanitizer`, `FilenameNormalizer`, `PatternLoader`, `SanitizerSettingsLoader`, `FileSystem`, plus interfaces for each |
| `FilenameSanitizerCli/` | Console app | `FilenameSanitizerCli <folder> [patterns]` |
| `FilenameSanitizer.Tests/` | xUnit | Covers the normalizer, pattern loader, settings loader and the sanitizer |

The library is also the NuGet package `FilenameSanitizer` — `GeneratePackageOnBuild` is on, so every build drops a `.nupkg` beside the DLL. It is **not** published to nuget.org, which is why anything referencing it as a package fails with `NU1101`.

## The casing rule

The library directory is `FilenameSanitizer/` — lowercase `n`, exactly. It was once `FileNameSanitizer/`, and a stale `ProjectReference` outlived the rename. On a case-sensitive filesystem, a reference whose casing does not match is **silently skipped during restore** and only surfaces later as a missing assembly at compile time.

Keep every `ProjectReference`, `Include` and `.sln` entry byte-for-byte identical to the on-disk name. The same hazard applies to any new project added to the solution.

## Build, test, run

From the repository root:

```bash
dotnet build FilenameTools.sln
dotnet test FilenameTools.sln
dotnet run --project FilenameSanitizerCli -- <folder> [patterns]
```

`-c Release` also builds: the CLI references the sibling project rather than a package, after a Release-only `PackageReference FilenameSanitizer 1.0.0` made `-c Release` fail with `NU1101`. Do not reintroduce it.

Where `$HOME` is not writable — the agent sandbox — source the cache redirect before restoring:

```bash
source local_dev/scripts/sandbox-env.sh
dotnet build FilenameTools.sln
```

Patterns are **semicolon-separated on the command line** and **new-line separated in the library API**; the CLI converts one to the other before calling `RenameFilesRemovingPatterns`. Passing new-line separated input on the CLI silently treats the whole string as one pattern.

The CLI waits for a keypress after printing the log path only when stdin is a TTY. Scripted and piped runs exit cleanly, so a test harness never hangs.

## The sanitisation contract

Three rules determine the output name, and all three are deliberate:

- **The invalid-character set is a union.** `Path.GetInvalidFileNameChars()` (OS-dependent) plus a fixed POSIX-unsafe list in `PatternLoader`, so the result is safe on Windows, Linux and macOS rather than only on the machine that produced it.
- **Windows reserved names are prefixed on every platform.** `CON`, `PRN`, `AUX`, `NUL`, `COM1-4` and `LPT1-4` become `_CON` and so on even on Linux, so a folder stays portable if it is later synced to Windows. Do not make this conditional on the host OS.
- **Normalisation is NFKD with an ASCII fallback**, followed by trimming and collapsing of repeated separators.

Configuration lives beside the library and is copied to output:

| File | Contents |
| :--- | :--- |
| `FilenameSanitizer/sanitizer_replace_patterns.txt` | Patterns to strip, one per line |
| `FilenameSanitizer/sanitizer_settings.json` | `ReplacementCharacter`, `ExcludedCharacters` |
| `FilenameSanitizer.log.<timestamp>.txt` | Audit trail written next to the processed folder; git-ignored |

### Platform detail

The sets behind the union, so a change to either side is checked against both:

- **Windows:** `<>:"/\|?*`, plus control characters, plus the reserved names above.
- **Linux and macOS:** `/` and the null byte.
- **Separators:** Windows writes `\`, the other two `/`; the sanitizer never emits a separator at all.

Filesystem operations are destructive here. Run the CLI against a scratch folder first, or take a backup:
a rename applied to the wrong pass is not undoable from the log.

## Known defect — pattern mode is gated by the wrong predicate

`FilenameNormalizer.RenameFileRemovingPatternsIfNeeded` (line 220) calls `ShouldRenameFile`, whose body asks `_sanitizer.IsFilenameSanitized(originalFilename)` (line 206). Any already-clean name satisfies that, so `prefix-test.txt` is logged as "already sanitized" instead of becoming `test.txt`.

The consequence: **patterns only take effect on files that also contain an invalid character.** The same gate sits in the ordinary sanitise path (line 157), so both are affected.

The fix is to compare the pattern-stripped name against the original instead of consulting `IsFilenameSanitized`. Nothing in the repository asserts the correct behaviour yet — a regression test belongs with the fix.

Do not describe the current behaviour as working around it: the CLI advertises pattern stripping, and this is a defect against that.

## The commit gate

The gate is machine-level. `core.hooksPath` is set globally to `~/.agents/githooks`, a link into the
prototype-k checkout, so every commit here runs that hook: `scripts/scan-publication-leaks.sh` first,
then `.agents/hooks/pre-commit-build-check.sh`. This repository carries no hook file of its own.

It exits 0 at once unless something staged matches `.cs`, `.csproj`, `.props`, `.targets`, `.sln` or
`.slnx`; otherwise it builds the solution (falling back to the nearest project), retries a failure once
for the Windows file lock, and exits 2 to block the commit with the compiler output as the reason. A
read-only workspace makes it warn and allow rather than block on `MSB3061`.

## Agent surfaces

`AGENTS.md` is the only agent configuration file in this repository. The skills, the hooks and the
shared rules are installed machine-wide from the prototype-k checkout (`~/.agents/skills/<name>`,
`~/.agents/hooks`, `~/.agents/githooks`), so there is no `.agents/` directory here to keep in step.

## Verification

```bash
dotnet build FilenameTools.sln          # exit 0, every error with file and line
dotnet test FilenameTools.sln           # the suite is the authority on the count
```

Run the CLI against a scratch directory before trusting a change with real files:

```bash
mkdir -p /tmp/sanitize-check && touch /tmp/sanitize-check/prefix-test.txt
dotnet run --project FilenameSanitizerCli -- /tmp/sanitize-check 'prefix-'
```

The documented defect means `prefix-test.txt` stays unchanged and the log says "already sanitized"; a fix must make it become `test.txt`.

## Known drift — do not rediscover these

| Claim or symptom | Reality | Action |
| :--- | :--- | :--- |
| README quotes a test count | Any count rots; the run is the measurement | Do not add a new count |
| `RepositoryUrl` in `FilenameSanitizer.csproj` points at the prototype-k umbrella | This package lives in this repository | Update it only with a version bump, since it is package metadata |
| `bin/`, `obj/`, `publish/` and `*.log` exist in the tree | Git-ignored build and run output | Never stage them |

## Where work is recorded

Investigation and research output goes to `local_user_files/`, which is git-ignored except for its README. A finding that only the owner can confirm stays an open item in the reply, not a claim in a file.

Documentation: this file is the agent entry point. The per-tool files (`.clinerules`, `.claudeignore`, `.github/copilot-instructions.md`, `.github/copilot/customization.json`, `.agents/settings.json`) were removed on 2026-11-11 so nothing duplicates it.
