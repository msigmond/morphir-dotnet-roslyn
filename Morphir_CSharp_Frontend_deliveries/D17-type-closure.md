# D17 — Object and remaining type forms

- status: planned
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D8](D08-enums-tuples.md), [D9](D09-user-data.md), [D10](D10-generics-dispatch.md), [D11](D11-collections.md), [D12](D12-patterns.md), [D13](D13-closures.md), [D14](D14-recursion.md), [D15](D15-loops-local-state.md), [D16](D16-framework-data.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Represent object, boxing/unboxing syntax, conversions, and the remaining fixed type catalog.
- Retain declared source type identity and resolved member targets.
- Do not enumerate runtime alternatives or simulate identity, storage, or boxing allocation.

## Acceptance

- Explicit casts remain casts. Implicit conversions add no execution steps.
- No value-domain or runtime-type proof is required.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

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
