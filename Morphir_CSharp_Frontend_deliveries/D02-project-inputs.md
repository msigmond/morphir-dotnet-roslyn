# D2 — Project and generated inputs

- status: passed under the approved Elm profile
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D1](D01-cli-record.md)
- evidence: [current gates](../docs/deliveries/elm-parity-state.md); [approved mappings](../docs/reviews/d1-d9-elm-mapping-decisions.md)

## Approved scope

Actual project/reference graphs, generated partial source, AdditionalFiles, analyzer settings, Debug/Release symbols, overflow options, and x64 input.

## Boundary

Compiler/generator failures and an invalid framework leave prior output unchanged.
Unmapped forms fail at the source. They produce no partial output.
No Source v1 output or custom source tag is accepted.

## Acceptance

- Every feature needs paired C#/Elm inputs and full JSON equality against `morphir-elm make`.
- Ignore only whitespace/object-key order. No package, name, type, or operation normalization.
- Keep the fixed SDK/reference/Roslyn versions, declared types, namespace selection, and atomic output.
- Run restore, build, the full suite, the D1–D9 catalog gate, and repository checks.
- Use the [case registry](../fixtures/elm-parity/cases.json) and executable rejection inputs.

The examples below are historical. They do not replace current compiler parity evidence.

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
