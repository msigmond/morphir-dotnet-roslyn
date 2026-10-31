# D12 — Patterns and switches

- status: planned
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D8](D08-enums-tuples.md), [D9](D09-user-data.md), [D10](D10-generics-dispatch.md), [D11](D11-collections.md)
- evidence: [exact-parity profile](../docs/deliveries/elm-parity-state.md); [remaining scope](../docs/elm-parity.md#existing-gaps)

## Prerequisite profile

Use the [D1–D9 Elm profile](../docs/deliveries/elm-parity-state.md) and all earlier exact-parity gates.
Each new mapping needs independent Elm input and complete JSON equality. Historical Source-mode passes do not count.

## Work

Scope below is a feature inventory. Confirm its standard Elm mapping before implementation.
No Source v1 tag or metadata envelope can satisfy this delivery.

- Keep switch statements/expressions, arms, guards, patterns, and source order.
- Keep binding variables and explicit exhaustiveness/default syntax.
- Do not expand patterns into runtime field tests or generated decision trees.

## Acceptance

- Every written pattern and branch is present.
- Compiler binding errors reject; runtime match-success proofs are not required.
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
namespace Example.D12;

public static class Rules
{
    public static int Category(int age) => age switch
    {
        >= 18 and < 65 => 1,
        >= 65 => 2,
        _ => 0
    };
}
```

### Elm — Example/D12/Rules.elm

```elm
module Example.D12.Rules exposing (category)

category : Int -> Int
category age =
    if age >= 18 && age < 65 then
        1
    else if age >= 65 then
        2
    else
        0
```
