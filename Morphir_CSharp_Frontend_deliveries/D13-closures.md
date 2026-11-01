# D13 — Anonymous functions and delegates

- status: planned
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D7](D07-calls.md), [D10](D10-generics-dispatch.md)
- evidence: [exact-parity profile](../docs/deliveries/elm-parity-state.md); [remaining scope](../docs/elm-parity.md#existing-gaps)

## Prerequisite profile

Use the [D1–D12 Elm profile](../docs/deliveries/D12-elm-parity.md) and all earlier exact-parity gates.
Each new mapping needs independent Elm input and complete JSON equality. Historical Source-mode passes do not count.

## Work

Scope below is a feature inventory. Confirm its standard Elm mapping before implementation.
No Source v1 tag or metadata envelope can satisfy this delivery.

- Keep lambda and anonymous-method parameters, bodies, captures, and lexical scopes.
- Keep delegate construction, combination, invocation, and explicit assignments where admitted.
- Do not create capture classes, receiver snapshots, or hidden lifetime state.

## Acceptance

- Mutable captures remain source references.
- No purity, effect, lifetime, or capture-ownership proof is required.
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
namespace Example.D13;

public static class Rules
{
    public static System.Func<int, bool> AtLeast(int minimum) =>
        age => age >= minimum;
}
```

### Elm — Example/D13/Rules.elm

```elm
module Example.D13.Rules exposing (atLeast)

atLeast : Int -> (Int -> Bool)
atLeast minimum =
    \age -> age >= minimum
```
