# D4 — Nullable values and operations

- status: passed under the approved Elm profile
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D3](D03-blocks-locals.md)
- evidence: [current gates](../docs/deliveries/elm-parity-state.md); [approved mappings](../docs/reviews/d1-d9-elm-mapping-decisions.md)

## Approved scope

Explicit nullable declarations use Maybe. Null uses Nothing; a plain value in a Maybe context uses Just. Coalesce uses withDefault; conditional access and mapped unary operations use map; lifted arithmetic uses map2. Presence tests compare with Nothing. Explicit scalar defaults use corresponding values.

## Boundary

Declared plain references stay plain. Inferred locals use the written initializer type, without nullable flow proofs. Nullable.Value, nullable flattening, and a nullable coalesce fallback remain rejected.
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

### C#

```csharp
namespace Example.D4;

public readonly record struct Customer(int? Age, string? Region);

public static class Rules
{
    public static bool Adult(Customer customer)
    {
        if (!customer.Age.HasValue)
            return false;
        return customer.Age.Value >= 18;
    }

    public static string RegionOrDefault(Customer customer) =>
        customer.Region ?? "unknown";
}
```

### Elm — Example/D4.elm

```elm
module Example.D4 exposing (Customer)

type alias Customer =
    { age : Maybe Int, region : Maybe String }
```

### Elm — Example/D4/Rules.elm

```elm
module Example.D4.Rules exposing (adult, regionOrDefault)

import Example.D4 exposing (Customer)

adult : Customer -> Bool
adult customer =
    case customer.age of
        Nothing ->
            False

        Just age ->
            age >= 18

regionOrDefault : Customer -> String
regionOrDefault customer =
    case customer.region of
        Nothing ->
            "unknown"

        Just region ->
            region
```
