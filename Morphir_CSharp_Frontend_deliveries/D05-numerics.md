# D5 — Numeric source structure

- status: partial exact parity; remaining mappings pending; historical Source-mode pass is not acceptance
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D3](D03-blocks-locals.md)
- evidence: [exact-parity profile](../docs/deliveries/elm-parity-state.md); [remaining scope](../docs/elm-parity.md#existing-gaps)

## Work

Scope below is a feature inventory. Confirm its standard Elm mapping before implementation.
No Source v1 tag or metadata envelope can satisfy this delivery.

- Use the numeric types emitted by the paired Elm project. Do not add source metadata to the output.
- Keep casts, constructors, shifts, named constants, and special floating values.
- Map ordinary arithmetic to one operation. Do not add limbs, wrapping, masking, saturation, or IEEE rounding.

## Acceptance

- Integer division, narrowing, finite-value cases, and Complex conversions need no runtime proof.
- No numeric compatibility module is emitted.
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
