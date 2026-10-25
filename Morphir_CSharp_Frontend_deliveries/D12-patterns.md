# D12 — Patterns and switches

- status: planned
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D8](D08-enums-tuples.md), [D9](D09-user-data.md), [D10](D10-generics-dispatch.md), [D11](D11-collections.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Keep switch statements/expressions, arms, guards, patterns, and source order.
- Keep binding variables and explicit exhaustiveness/default syntax.
- Do not expand patterns into runtime field tests or generated decision trees.

## Acceptance

- Every written pattern and branch is present.
- Compiler binding errors reject; runtime match-success proofs are not required.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

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
