# D6 — Decimal values and operations

- status: passed under the approved Elm profile
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D4](D04-maybe.md), [D5](D05-numerics.md)
- evidence: [current gates](../docs/deliveries/elm-parity-state.md); [approved mappings](../docs/reviews/d1-d9-elm-mapping-decisions.md)

## Approved scope

Decimal literals use fromFloat. Scalar constructors and listed methods/conversions/comparisons use direct SDK operations. Division, including Decimal.Divide, uses Decimal.div in an explicit nullable Decimal result context.

## Boundary

Plain Decimal division and unlisted precision, rounding-mode, scale, constructor, and conversion forms remain rejected. No fallback or range proof is added.
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
