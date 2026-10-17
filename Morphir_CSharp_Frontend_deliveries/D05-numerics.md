# D5 — Numeric domains, conversions, unchecked arithmetic

- status: passed
- evidence: [D5 execution/gates](../docs/deliveries/D5-state.md)
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D3](D03-blocks-locals.md)
- outcome: Exact admitted integer/floating behavior; unchecked wrapping
- ownership: Numeric mappings/helpers; range/failure proofs; transport representations
- fixture-root: `fixtures/D5/`; test trait: `Delivery=D5`

## Implementation

- [x] CurrentSupportedExample update: every delivered numeric family, promotions/casts/operators, unchecked wrapping, exact transport domains
- [x] byte/sbyte/short/ushort/int/uint/long/ulong; fixed 64-bit nint/nuint; float/double
- [x] System.Half/Int128/UInt128; System.Numerics.BigInteger/Complex; Decimal → D6
- [x] Literal domains; promotion; casts; arithmetic; division/remainder; bitwise; shifts; comparison
- [x] C# unchecked wrap/promotions; floating precision, signed zero, NaN/infinities and edge values
- [x] Explicit exact representations when JSON numbers/SDK domains insufficient; wide-integer precision/transport checks
- [x] Division/remainder + conversion failure proofs; exceptional signed-division boundary; caught recovery deferred to D18.1
- [x] Checked syntax/operations/conversions: rejection; project overflow settings retained

## Model increment

- Numeric literal cases; valid-IR exact wide/non-finite representations; helper references

## Fixture contract

- package: `Example`; namespace: `Example.D5`
- source data paths: —
- member modules: `Example.D5.Rules`
- constraints: Ordinary Elm Int/Float arithmetic alone insufficient for C# numeric compatibility

## Acceptance

- [x] Next(int.MaxValue) → int.MinValue; example domain exactly C# int range
- [x] Every numeric family: boundary/promotions/conversions/unchecked fixtures; complete helper definitions/specifications in paired Elm
- [x] Wide signed/unsigned/native types: full-domain transport evidence; float/Half/non-finite cases: explicit encoding evidence
- [x] Constant-safe checked expressions, checked blocks/operators, compiler-enabled checked operations → rejection

## Rejections

- [x] Explicit checked syntax; checked semantic operation/conversion
- [x] Unproved division/remainder/conversion failure path; precision-losing representation
- [x] Incompatible 32-bit native target

## Code examples

### C#

```csharp
namespace Example.D5;

public static class Rules
{
    public static int Next(int value) => unchecked(value + 1);
}
```

### Elm — Example/D5/Rules.elm

```elm
module Example.D5.Rules exposing (next)

next : Int -> Int
next value =
    if value == 2147483647 then
        -2147483648
    else
        value + 1
```
