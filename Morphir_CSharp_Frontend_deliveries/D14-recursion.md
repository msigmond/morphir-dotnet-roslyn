# D14 — Recursive binding coverage

- status: planned
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D7](D07-calls.md), [D13](D13-closures.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Extend delivered direct recursive references to all scoped generic/local/closure combinations.
- Keep self and mutual call targets without expanding bodies.
- Validate symbol identity and nesting, not termination or runtime SCC closure.

## Acceptance

- Recursive declarations and calls round-trip through source metadata.
- No recursive helper is generated to emulate runtime behavior.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

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
