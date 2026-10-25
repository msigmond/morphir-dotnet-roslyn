# D11 — Arrays and collection structure

- status: planned
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D4](D04-maybe.md), [D10](D10-generics-dispatch.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Keep collection types, element types, rank, initializers, indexing, and named members.
- Keep explicit collection updates and collection expressions.
- Do not build persistent-state algorithms, null defaults, bounds guards, enumeration machinery, or backing storage.

## Acceptance

- Array and collection calls remain written operations.
- No nonzero-length, index-range, ownership, or immutable-backing proof is required.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

## Historical examples

### C#

```csharp
namespace Example.D11;

public static class Rules
{
    public static int FirstOrZero(int[]? values)
    {
        if (values is null || values.Length == 0)
            return 0;
        return values[0];
    }

    public static int Count(System.Collections.Generic.List<int>? values) =>
        values?.Count ?? 0;
}
```

### Elm — Example/Compat/Array1.elm

```elm
module Example.Compat.Array1 exposing (Array1)

type alias Array1 a =
    { length : Int, items : List a }
```

### Elm — Example/D11/Rules.elm

```elm
module Example.D11.Rules exposing (count, firstOrZero)

import Example.Compat.Array1 exposing (Array1)

firstOrZero : Maybe (Array1 Int) -> Int
firstOrZero possibleArray =
    case possibleArray of
        Nothing ->
            0

        Just array ->
            case array.items of
                [] ->
                    0

                first :: _ ->
                    first

count : Maybe (List Int) -> Int
count possibleList =
    case possibleList of
        Nothing ->
            0

        Just values ->
            List.length values
```
