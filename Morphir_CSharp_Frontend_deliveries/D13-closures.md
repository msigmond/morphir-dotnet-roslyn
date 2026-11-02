# D13 — Anonymous functions and delegates

- status: passed under the approved anonymous-function profile
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D7](D07-calls.md), [D10](D10-generics-dispatch.md)
- evidence: [D13 validation](../docs/deliveries/D13-elm-parity.md); [remaining scope](../docs/elm-parity.md#existing-gaps)

## Prerequisite profile

Use the [D1–D12 Elm profile](../docs/deliveries/D12-elm-parity.md) and all earlier exact-parity gates.
Each new mapping needs independent Elm input and complete JSON equality. Historical Source-mode passes do not count.

## Work

The user approved the [anonymous-function profile](../docs/mappings/D13.md) on 2026-11-01.

- Keep lambda and anonymous-method parameters, bodies, captures, and lexical scopes.
- Map explicit Func/Action construction to function values and invocation to application.
- Match the Elm compiler's leading-lambda definition inputs.
- Keep mutation, multicast delegates, async/ref callbacks, and custom delegate declarations rejected.
- Do not create capture classes, receiver snapshots, or hidden lifetime state.

D13 is a prerequisite for the [D11 collection-operation follow-up](D11-collections.md#collection-operation-follow-up). Filter, map, and fold calls belong to that follow-up.

## Acceptance

- Immutable captures use lexical references. Mutation remains rejected.
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
