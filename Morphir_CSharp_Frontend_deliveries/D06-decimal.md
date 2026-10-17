# D6 — Exact Decimal construction and safety

- status: passed
- evidence: [implementation and gates](../docs/deliveries/D6-state.md)
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D4](D04-maybe.md), [D5](D05-numerics.md)
- outcome: Exact 0.1m + C#-compatible proven-safe calculations
- ownership: Decimal mappings/helpers; D3 safety proofs; Decimal codec
- fixture-root: `fixtures/D6/`; test trait: `Delivery=D6`

## Implementation

- [x] CurrentSupportedExample update: exact construction, guarded arithmetic/conversions, rounding/scale within proven-safe domains
- [x] Exact decimal construction; no binary-float intermediate; construction dependencies recorded
- [x] SDK fromString → Maybe Decimal; constant validity + M1 extraction proof; no fallback default
- [x] Arithmetic/comparison/casts/rounding/observable scale/range; SDK semantics verified or compatibility helpers
- [x] Failure proof: overflow, zero division, conversions, supported-member preconditions; nonzero divisor alone insufficient
- [x] Proof propagation through branches/local updates/calls; constants + validated bounds + dominating guards
- [x] Business return types retained; no Result/Maybe error wrapper, saturation, invented defaults or differing arithmetic adoption

## Model increment

- DecimalLiteral serialization; Decimal construction/helper references

## Fixture contract

- package: `Example`; namespace: `Example.D6`
- source data paths: —
- member modules: `Example.D6.Rules`
- constraints: Policy D3 ≠ delivery D3; selected policy remains mandatory after D18.1

## Acceptance

- [x] 3 units → exact 0.3; sample result domain 0..100; tenth 1 construction exact
- [x] Constants + guarded bounds → success; full-distribution parity with identical construction/arithmetic
- [x] C# rounding/conversion/scale boundaries → behavior evidence
- [x] Zero divisor, possible overflow, unproved safety → rejection, including within catch
- [x] Decimal literal codec: independent Elm fixture even when source literals lowered to calls

## Rejections

- [x] Unproved Decimal failure safety; undocumented business bounds
- [x] Checked syntax; binary-floating construction; error-wrapper substitution

## Code examples

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
