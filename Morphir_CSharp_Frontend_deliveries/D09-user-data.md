# D9 — Data declarations, members, and state

- status: passed under the source contract; revalidated on 2026-10-24
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D7](D07-calls.md), [D8](D08-enums-tuples.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Keep all six data forms, primary parameters, source fields/properties, and explicit constructors/accessors.
- Keep new, object initializers, member paths, assignments, with, operators, and equality.
- Keep aliases and recursive references. Do not simulate ownership, copies, object graphs, constructor execution, or synthesized equality.

## Acceptance

- Input mutation, identity observations, cycles, and recursive calls need no runtime proof.
- No hidden primary fields, generated defaults, compatibility equality helpers, or inlined bodies appear.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

## Historical examples

### C#

```csharp
namespace Example.D9;

public sealed class Counter
{
    public int Value;
}

public static class Rules
{
    public static int BuildAndUpdate()
    {
        var counter = new Counter { Value = 1 };
        var alias = counter;
        alias.Value = 2;
        return counter.Value;
    }
}
```

### Elm — Example/D9.elm

```elm
module Example.D9 exposing (Counter)

type alias Counter =
    { value : Int }
```

### Elm — Example/D9/Rules.elm

```elm
module Example.D9.Rules exposing (buildAndUpdate)

import Example.D9 exposing (Counter)

buildAndUpdate : Int
buildAndUpdate =
    let
        counterBefore : Counter
        counterBefore =
            { value = 1 }

        counterAfter =
            { counterBefore | value = 2 }
    in
    counterAfter.value
```
