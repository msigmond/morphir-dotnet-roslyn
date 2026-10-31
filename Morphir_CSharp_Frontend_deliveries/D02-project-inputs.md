# D2 — Project and generated source inputs

- status: partial exact parity; remaining mappings pending; historical Source-mode pass is not acceptance
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D1](D01-cli-record.md)
- evidence: [exact-parity profile](../docs/deliveries/elm-parity-state.md); [remaining scope](../docs/elm-parity.md#existing-gaps)

## Work

Scope below is a feature inventory. Confirm its standard Elm mapping before implementation.
No Source v1 tag or metadata envelope can satisfy this delivery.

- Keep actual project references, compilation settings, AdditionalFiles, analyzer configuration, and generated syntax.
- Combine partial declarations by symbol. Retain source order and each declaration location.
- Keep checked compiler settings as input facts. Do not insert overflow operations.

## Acceptance

- Project and source-file CLI modes emit the same selected structure.
- Compiler and generator failures preserve any existing output.
- Every feature needs paired C#/Elm inputs and full JSON equality against `morphir-elm make`.
- Ignore only whitespace/object-key order. No package, name, type, or operation normalization.
- Ask the user if the Elm counterpart is unclear. No unrequested output or feature is permitted.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context. New acceptance references must come from `morphir-elm make`.

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
