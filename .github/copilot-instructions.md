# GitHub Copilot — Custom Instructions (FilenameSanitizer)

**Note:** This section is designed for small C#/.NET utilities and test projects within the `FilenameSanitizer` folder.
- These instructions complement the root `.github/copilot-instructions.md`. Use the root guidance first; apply these C#/.NET specifics for files inside this folder.

## Purpose
- Provide guidance tailored to the `FilenameSanitizer` C#/.NET codebase and its test projects.

## Additional/override guidelines (in addition to root)
- Primary language: C# (.NET). Follow existing project conventions (naming, dependency versions, solution layout).
- Keep public APIs stable; prefer adding new methods rather than changing existing signatures unless necessary.
- Match the project's test framework and style; ensure tests run on CI and locally.
- Avoid adding unnecessary third-party dependencies; prefer the standard library and small, well-maintained packages.
- When editing `*.csproj` files, keep SDK, target frameworks, and package references consistent with the solution.
