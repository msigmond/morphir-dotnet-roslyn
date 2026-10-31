# D6 — Decimal source structure

- status: partial exact parity; remaining mappings pending; historical Source-mode pass is not acceptance
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D4](D04-maybe.md), [D5](D05-numerics.md)
- evidence: [exact-parity profile](../docs/deliveries/elm-parity-state.md); [remaining scope](../docs/elm-parity.md#existing-gaps)

## Work

Scope below is a feature inventory. Confirm its standard Elm mapping before implementation.
No Source v1 tag or metadata envelope can satisfy this delivery.

- Use SDK Decimal type and direct decimal literals.
- Map add, subtract, and multiply to direct SDK references. Confirm an Elm mapping for each other operation.
- Keep explicit construction, conversions, scale, rounding arguments, and named members.

## Acceptance

- Unguarded decimal multiplication and division are accepted.
- No coefficient records, epsilon arguments, parsing, range proofs, or Decimal helper modules appear.
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
