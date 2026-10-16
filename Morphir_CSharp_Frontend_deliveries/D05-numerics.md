# D5 — Numeric domains, conversions, unchecked arithmetic

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D3](D03-blocks-locals.md)
- outcome: Exact admitted integer/floating behavior; unchecked wrapping
- ownership: Numeric mappings/helpers; range/failure proofs; transport representations
- fixture-root: `fixtures/D5/`; test trait: `Delivery=D5`

## Implementation

- [ ] CurrentSupportedExample update: every delivered numeric family, promotions/casts/operators, unchecked wrapping, exact transport domains
- [ ] byte/sbyte/short/ushort/int/uint/long/ulong; fixed 64-bit nint/nuint; float/double
- [ ] System.Half/Int128/UInt128; System.Numerics.BigInteger/Complex; Decimal → D6
- [ ] Literal domains; promotion; casts; arithmetic; division/remainder; bitwise; shifts; comparison
- [ ] C# unchecked wrap/promotions; floating precision, signed zero, NaN/infinities and edge values
- [ ] Explicit exact representations when JSON numbers/SDK domains insufficient; wide-integer precision/transport checks
- [ ] Division/remainder + conversion failure proofs; exceptional signed-division boundary; caught recovery deferred to D18.1
- [ ] Checked syntax/operations/conversions: rejection; project overflow settings retained

## Model increment

- Numeric literal cases; valid-IR exact wide/non-finite representations; helper references

## Fixture contract

- package: `Example`; namespace: `Example.D5`
- source data paths: —
- member modules: `Example.D5.Rules`
- constraints: Ordinary Elm Int/Float arithmetic alone insufficient for C# numeric compatibility

## Acceptance

- [ ] Next(int.MaxValue) → int.MinValue; example domain exactly C# int range
- [ ] Every numeric family: boundary/promotions/conversions/unchecked fixtures; complete helper definitions/specifications in paired Elm
- [ ] Wide signed/unsigned/native types: full-domain transport evidence; float/Half/non-finite cases: explicit encoding evidence
- [ ] Constant-safe checked expressions, checked blocks/operators, compiler-enabled checked operations → rejection

## Rejections

- [ ] Explicit checked syntax; checked semantic operation/conversion
- [ ] Unproved division/remainder/conversion failure path; precision-losing representation
- [ ] Incompatible 32-bit native target

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
