# D17 — Object and remaining type forms

- status: planned
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D8](D08-enums-tuples.md), [D9](D09-user-data.md), [D10](D10-generics-dispatch.md), [D11](D11-collections.md), [D12](D12-patterns.md), [D13](D13-closures.md), [D14](D14-recursion.md), [D15](D15-loops-local-state.md), [D16](D16-framework-data.md)
- evidence: [exact-parity profile](../docs/deliveries/elm-parity-state.md); [remaining scope](../docs/elm-parity.md#existing-gaps)

## Prerequisite profile

Use the [D1–D9 Elm profile](../docs/deliveries/elm-parity-state.md) and all earlier exact-parity gates.
Each new mapping needs independent Elm input and complete JSON equality. Historical Source-mode passes do not count.

## Work

Scope below is a feature inventory. Confirm its standard Elm mapping before implementation.
No Source v1 tag or metadata envelope can satisfy this delivery.

- Represent object, boxing/unboxing syntax, conversions, and the remaining fixed type catalog.
- Retain declared source type identity and resolved member targets.
- Do not enumerate runtime alternatives or simulate identity, storage, or boxing allocation.

## Acceptance

- Explicit casts remain casts. Implicit conversions add no execution steps.
- No value-domain or runtime-type proof is required.
- Every feature needs paired C#/Elm inputs and full JSON equality against `morphir-elm make`.
- Ignore only whitespace/object-key order. No package, name, type, or operation normalization.
- Ask the user if the Elm counterpart is unclear. No unrequested output or feature is permitted.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context. New acceptance references must come from `morphir-elm make`.

## Historical examples

### C#

```csharp
namespace Example.D17;

public static class Rules
{
    public static int AgeOrZero(object? value) =>
        value is int age ? age : 0;
}
```

### Elm — Example/Compat/Object.elm

```elm
module Example.Compat.Object exposing (Boxed(..))

type Boxed
    = BoxedInt Int
    | BoxedBool Bool
```

### Elm — Example/D17/Rules.elm

```elm
module Example.D17.Rules exposing (ageOrZero)

import Example.Compat.Object exposing (Boxed(..))

ageOrZero : Maybe Boxed -> Int
ageOrZero possibleValue =
    case possibleValue of
        Just (BoxedInt age) ->
            age

        _ ->
            0
```
