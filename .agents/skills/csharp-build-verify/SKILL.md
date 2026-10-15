---
name: csharp-build-verify
description: Build the affected .NET solution and recover from the build failures these repositories actually hit.
---

# Build verification

Once `.cs`, `.csproj` or `.sln` files change, the affected solution must build before the work
counts as done.

Build only what the change touches. A repository usually has one solution at its root; a library
with no solution of its own compiles as a dependency of the projects that reference it:

```bash
dotnet build <Solution>.sln --no-incremental
```

`--no-incremental` matters: it stops a stale `obj/` from hiding errors. Where the repository ships
a build script it covers every solution at once — `scripts/build-all-linux.sh --test`, `-c Release`
for Release. `dotnet build` restores on its own, so do not run `dotnet restore` first.

## Recovery

**Null-byte corruption** — `MSB5010`, or `CS1056 (Unexpected character '\0')`, means the file
contains a `\0`. Restore it from git rather than editing it:

```bash
git show HEAD:path/to/file.cs > path/to/file.cs
```

Confirm no `\0` remains, then rebuild. A corrupted file is never worth repairing by hand.

**`MSB4011`** — a project explicitly `<Import>`s `Directory.Build.props` or
`Directory.Build.targets`. The SDK already imports them; delete the `<Import>`. The only
exception is a legacy `<Project>` without `Sdk=`, which gets no automatic import.

**Missing types after a rename or move** — on Linux a wrong-case `ProjectReference`, `Include` or
`.sln` entry is *silently skipped* at restore and only fails later as a compile error. Match the
on-disk casing exactly.

Warnings do not block; errors do. Report every error with its file and line number.
