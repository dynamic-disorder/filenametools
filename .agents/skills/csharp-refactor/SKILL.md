---
name: csharp-refactor
description: Refactor C# code without changing behaviour — structure, naming and duplication only.
disable-model-invocation: true
---

# Refactor

Refactor what the request names.

Assume .NET/C# fluency. Aim at long methods, mixed concerns, poor names, duplication and tight
coupling; prefer small well-named members and dependency injection over statics. Don't abstract
prematurely, and don't move logic across component boundaries.

## Working rules

- Behaviour must not change. If it has to, stop and say so before touching the code.
- State each change in one line — what and why — before making it.
- Move in small steps rather than one large rewrite.
- No comments that merely restate the code.
- If the target has no tests and the refactor is risky, flag that instead of proceeding.
