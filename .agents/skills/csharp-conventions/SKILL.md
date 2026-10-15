---
name: csharp-conventions
description: Non-obvious C# style rules these repositories follow that .editorconfig and the analyzers do not catch.
---

# C# conventions

`.editorconfig` is the source of truth for formatting, spacing, `var`, modifier order and
analyzer severities — read it instead of restating it here. Only these rules are convention-only,
silently unenforced, or were stated wrongly in the past.

## Tooling will not flag these

**No expression-bodied members.** Every `csharp_style_expression_bodied_*` is `false:none`, so
analyzers stay silent. Use block bodies even for one-liners. Lambdas (`x => x.Name`) are
unaffected. An existing thin delegating wrapper is the one accepted exception; leave it alone.

**Null checks.** Prefer `is null` / `is not null` over `== null` / `!= null`. `.editorconfig`
enforces that only for `ReferenceEquals` (`IDE0041`), not for `==`.

**XML docs.** Production code documents public and internal types *and* their members, so put
`/// <summary>` on what you add or change and leave untouched members alone. Test projects
differ: document the test class only, never individual `[Fact]` methods.

## Correcting a stale rule

Use the C# keyword for BCL types, including on member access: `int.Parse(...)`,
`string.IsNullOrEmpty(...)`, `bool.TryParse(...)`. `.editorconfig` sets
`dotnet_style_predefined_type_for_member_access = true`, so `Int32.Parse` / `String.IsNullOrEmpty`
are the wrong way round.

Async methods likewise keep the `Async` suffix — it is the codebase norm, not a name to "fix".

## Enforced already, but worth knowing

These surface as warnings: braces on every block even when single-line (`IDE0011`), no `this.`
qualification, `_camelCase` private fields, file-scoped namespaces, and `using` directives
grouped `System.*`/`Microsoft.*` → third-party → own namespaces with a blank line between groups.
The third-party/own split is convention only.
