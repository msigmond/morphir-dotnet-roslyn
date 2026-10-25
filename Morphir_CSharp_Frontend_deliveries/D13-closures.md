# D13 — Anonymous functions and delegates

- status: planned
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D7](D07-calls.md), [D10](D10-generics-dispatch.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Keep lambda and anonymous-method parameters, bodies, captures, and lexical scopes.
- Keep delegate construction, combination, invocation, and explicit assignments where admitted.
- Do not create capture classes, receiver snapshots, or hidden lifetime state.

## Acceptance

- Mutable captures remain source references.
- No purity, effect, lifetime, or capture-ownership proof is required.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

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
