# D14 — Recursive binding coverage

- status: planned
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D7](D07-calls.md), [D13](D13-closures.md)
- evidence: [exact-parity profile](../docs/deliveries/elm-parity-state.md); [remaining scope](../docs/elm-parity.md#existing-gaps)

## Prerequisite profile

Use the [D1–D10 Elm profile](../docs/deliveries/D10-elm-parity.md) and all earlier exact-parity gates.
Each new mapping needs independent Elm input and complete JSON equality. Historical Source-mode passes do not count.

## Work

Scope below is a feature inventory. Confirm its standard Elm mapping before implementation.
No Source v1 tag or metadata envelope can satisfy this delivery.

- Extend delivered direct recursive references to all scoped generic/local/closure combinations.
- Keep self and mutual call targets without expanding bodies.
- Validate symbol identity and nesting, not termination or runtime SCC closure.

## Acceptance

- Recursive declarations and calls match the paired Elm output.
- No recursive helper is generated to emulate runtime behavior.
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
namespace Example.D14;

public static class Rules
{
    public static bool Even(int value) =>
        value <= 0 || Odd(value - 1);

    public static bool Odd(int value) =>
        value > 0 && Even(value - 1);
}
```

### Elm — Example/D14/Rules.elm

```elm
module Example.D14.Rules exposing (even, odd)

even : Int -> Bool
even value =
    if value <= 0 then
        True
    else
        odd (value - 1)

odd : Int -> Bool
odd value =
    if value > 0 then
        even (value - 1)
    else
        False
```
