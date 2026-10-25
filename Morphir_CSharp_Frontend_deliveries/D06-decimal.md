# D6 — Decimal source structure

- status: passed under the source contract; revalidated on 2026-10-24
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D4](D04-maybe.md), [D5](D05-numerics.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Use SDK Decimal type and direct decimal literals.
- Map add, subtract, and multiply to direct SDK references. Keep other operations as source operations.
- Keep explicit construction, conversions, scale, rounding arguments, and named members.

## Acceptance

- Unguarded decimal multiplication and division are accepted.
- No coefficient records, epsilon arguments, parsing, range proofs, or Decimal helper modules appear.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

## Historical examples

### C#

```csharp
namespace Example.D6;

public static class Rules
{
    public static decimal Price(int units)
    {
        if (units < 0 || units > 1000)
            return 0m;
        return units * 0.1m;
    }
}
```

### Elm — Example/D6/Rules.elm

```elm
module Example.D6.Rules exposing (price)

import Morphir.SDK.Decimal as Decimal exposing (Decimal)

price : Int -> Decimal
price units =
    if units < 0 || units > 1000 then
        Decimal.zero
    else
        Decimal.mul (Decimal.fromInt units) (Decimal.tenth 1)
```
