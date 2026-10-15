---
name: unit-test-writer
description: Write xUnit tests with Shouldly assertions and NSubstitute substitutes for the C# projects that have tests.
---

# unit-test-writer

xUnit, **Shouldly** for assertions, **NSubstitute** for substitutes. **Moq is banned** (its
SponsorLink history) — use `Substitute.For<T>()` or a hand-written fake. Confirm against the
target test project's package references before adding a new dependency.

## Where tests live

Flat beside the code in a sibling `{Project}.Tests/` directory — these repositories do not mirror
production subfolders — with namespace `{Project}.Tests`. A class in `{Project}/Thing.cs` is
covered by `{Project}.Tests/ThingTests.cs`.

Directory casing must match the disk exactly: a wrong-case `ProjectReference` is silently skipped
at restore on Linux.

## Conventions

- `public sealed class <ClassName>Tests` in `<ClassName>Tests.cs`; one `/// <summary>` on the class,
  not on individual test methods.
- Test names: `MethodName_StateUnderTest_ExpectedBehaviour`. The underscores are why each test
  `.csproj` carries `<NoWarn>CA1707</NoWarn>`.
- `// Given:` / `// When:` / `// Then:` sections; `[Theory]` + `[InlineData]` for parameterized cases.
- Name the system under test `sut`, built by a small scenario-specific factory rather than a field.
- Assert observable outcomes; reach for `Received()` mainly on error paths where there is no state
  to observe. Prefer one hand-written fake over repeated substitute setup shared by many tests.
- Don't run the tests unless asked.

```csharp
namespace Parser.Tests;

/// <summary>Covers parsing a row and reporting the fields it found.</summary>
public sealed class ParserTests
{
    private readonly IStore _store = Substitute.For<IStore>();

    [Fact]
    public void Parse_DirtyName_ReturnsSanitizedForm()
    {
        // Given: a row whose value needs normalising
        _store.GetRows(Arg.Any<string>()).Returns(["bad name"]);
        var sut = CreateParser();

        // When: the parser runs
        sut.Parse();

        // Then: the value was normalised before it was stored
        _store.Received().Save("bad name", "bad_name");
    }
}
```
