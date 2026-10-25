# D4 — Explicit nullability and nullable syntax

- status: passed under the source contract; revalidated on 2026-10-24
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D3](D03-blocks-locals.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Map an explicit nullable type to Maybe, including nested slots and nullable analysis disabled.
- Keep null, !, nullable casts, .Value, HasValue, ??, ??=, and ?. as written.
- Use declared return types. Do not infer nullable types from values.

## Acceptance

- Nonnullable string and Loan inputs do not become Maybe.
- No presence proof, extracted-value match, or invented fallback is required.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

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
