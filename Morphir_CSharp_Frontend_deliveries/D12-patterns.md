# D12 — Patterns and switches

- status: passed under the approved direct case profile
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D8](D08-enums-tuples.md), [D9](D09-user-data.md), [D10](D10-generics-dispatch.md), [D11](D11-collections.md)
- evidence: [exact-parity profile](../docs/deliveries/elm-parity-state.md); [remaining scope](../docs/elm-parity.md#existing-gaps)

## Prerequisite profile

Use the [D1–D11 Elm profile](../docs/deliveries/D11-elm-parity.md) and all earlier exact-parity gates.
Each new mapping needs independent Elm input and complete JSON equality. Historical Source-mode passes do not count.

## Work

The user approved the [direct case profile](../docs/mappings/D12.md) on 2026-11-01.

- Map returned switches to standard PatternMatch in source order.
- Map approved literals, enum values, var/discard, tuples, and list prefix/tail patterns directly.
- Keep guards, relational/logical/property/type patterns, grouped labels, and fall-through forms rejected.
- Add no pattern expansion, decision tree, fallback, or runtime match proof.

## Acceptance

- Every written pattern and branch is present.
- Compiler binding errors reject; runtime match-success proofs are not required.
- Every feature needs paired C#/Elm inputs and full JSON equality against `morphir-elm make`.
- Ignore only whitespace/object-key order. No package, name, type, or operation normalization.
- Ask the user if the Elm counterpart is unclear. No unrequested output or feature is permitted.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context. The relational-pattern expansion below is not an approved mapping.
New acceptance references must come from `morphir-elm make`.

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
