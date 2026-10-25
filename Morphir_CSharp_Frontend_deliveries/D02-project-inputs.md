# D2 — Project and generated source inputs

- status: passed under the source contract; revalidated on 2026-10-24
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D1](D01-cli-record.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Keep actual project references, compilation settings, AdditionalFiles, analyzer configuration, and generated syntax.
- Combine partial declarations by symbol. Retain source order and each declaration location.
- Keep checked compiler settings as input facts. Do not insert overflow operations.

## Acceptance

- Project and source-file CLI modes emit the same selected structure.
- Compiler and generator failures preserve any existing output.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

## Historical examples

### C# — Business/Rules.cs

```csharp
namespace Example.D2.Business;

public static partial class Rules
{
    private static bool Adult(int age) =>
        Example.D2.Shared.AgeRule.Adult(age);

    public static bool Eligible(int age) => Adult(age);
}
```

### C# — Shared/AgeRule.cs

```csharp
namespace Example.D2.Shared;

public static class AgeRule
{
    public static bool Adult(int age) => age >= 18;
}
```

### C# — generated Business/Rules.Generated.cs

```csharp
namespace Example.D2.Business;

public static partial class Rules
{
    public static int GeneratedThreshold() => 18;
}
```

### Elm — Example/D2/Shared/AgeRule.elm

```elm
module Example.D2.Shared.AgeRule exposing (adult)

adult : Int -> Bool
adult age =
    age >= 18
```

### Elm — Example/D2/Business/Rules.elm

```elm
module Example.D2.Business.Rules exposing (eligible, generatedThreshold)

import Example.D2.Shared.AgeRule as AgeRule

adult : Int -> Bool
adult age =
    AgeRule.adult age

eligible : Int -> Bool
eligible age =
    adult age

generatedThreshold : Int
generatedThreshold =
    18
```
