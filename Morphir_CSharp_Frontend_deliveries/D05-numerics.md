# D5 — Numeric source structure

- status: passed under the source contract; revalidated on 2026-10-24
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D3](D03-blocks-locals.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Use simple numeric types. Keep source width and signedness in syntax metadata.
- Keep casts, constructors, shifts, named constants, and special floating values.
- Map ordinary arithmetic to one operation. Do not add limbs, wrapping, masking, saturation, or IEEE rounding.

## Acceptance

- Integer division, narrowing, finite-value cases, and Complex conversions need no runtime proof.
- No numeric compatibility module is emitted.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

## Historical examples

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
