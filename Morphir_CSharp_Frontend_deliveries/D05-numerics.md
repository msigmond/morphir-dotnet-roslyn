# D5 — Numeric types and written operations

- status: passed under the approved Elm profile
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D3](D03-blocks-locals.md)
- evidence: [current gates](../docs/deliveries/elm-parity-state.md); [approved mappings](../docs/reviews/d1-d9-elm-mapping-decisions.md)

## Approved scope

Int carriers for delivered integer widths, Float carriers for Half/Single/Double, direct arithmetic/comparisons, written mapped casts, integer remainder, and listed SDK members/constants.

## Boundary

Complex, unlisted constants/constructors/conversions, and bit operations without SDK mappings remain rejected. No wrap, range, finite-value, or overflow emulation is added.
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
