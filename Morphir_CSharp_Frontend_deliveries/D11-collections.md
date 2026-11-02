# D11 — Arrays and collection structure

- status: passed under the approved collection profile
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D4](D04-maybe.md), [D10](D10-generics-dispatch.md)
- evidence: [exact-parity profile](../docs/deliveries/elm-parity-state.md); [remaining scope](../docs/elm-parity.md#existing-gaps)

## Prerequisite profile

Use the [D1–D10 Elm profile](../docs/deliveries/D10-elm-parity.md) and all earlier exact-parity gates.
Each new mapping needs independent Elm input and complete JSON equality. Historical Source-mode passes do not count.

## Work

The user approved the [collection profile](../docs/mappings/D11.md) on 2026-11-01.

- Map approved carriers to List, Dict, and Set.
- Map explicit elements, spreads, constructors/factories, and named reads to standard Elm forms.
- Permit indexing only with a declared nullable result. Add no fallback or bounds guard.
- Keep all excluded carriers, mutations, and unlisted overloads rejected.

## Acceptance

- Array and collection calls remain written operations.
- No nonzero-length, index-range, ownership, or immutable-backing proof is required.
- Every feature needs paired C#/Elm inputs and full JSON equality against `morphir-elm make`.
- Ignore only whitespace/object-key order. No package, name, type, or operation normalization.
- Ask the user if the Elm counterpart is unclear. No unrequested output or feature is permitted.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

## Collection-operation follow-up

- status: passed under the approved collection-operation profile
- prerequisites: passed [D11](../docs/deliveries/D11-elm-parity.md) and [D13](D13-closures.md) exact-parity gates
- scope: collection filter, map, and fold calls, including LINQ `Where`, `Select`, and `Aggregate`, plus `List.FindAll` and `List.ConvertAll`

The user approved this assignment and the [call mappings](../docs/mappings/D11.md#collection-operation-follow-up) on 2026-11-01.
D13 supplies anonymous functions and delegates. This follow-up supplies the collection operations.

Map single-parameter filter/map callbacks, seeded Aggregate with the approved argument-order adapter, and ToList/ToArray.
Keep indexed callbacks, unseeded Aggregate, result selectors, query syntax, and unlisted operations rejected.
Each admitted operation and overload needs equivalent C#/Elm inputs and complete JSON equality against pinned `morphir-elm make` output.
Test callback parameters, captured values, result types, and chained calls for each admitted form. Keep unapproved forms explicitly rejected.
The sample, catalogs, mapping, and [delivery evidence](../docs/deliveries/D11-follow-up-elm-parity.md) are updated.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context. New acceptance references must come from `morphir-elm make`.

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
