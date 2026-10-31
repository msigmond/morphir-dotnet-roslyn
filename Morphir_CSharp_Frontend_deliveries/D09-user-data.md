# D9 — Data declarations, members, and state

- status: partial exact parity; remaining mappings pending; historical Source-mode pass is not acceptance
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D7](D07-calls.md), [D8](D08-enums-tuples.md)
- evidence: [exact-parity profile](../docs/deliveries/elm-parity-state.md); [remaining scope](../docs/elm-parity.md#existing-gaps)

## Work

Scope below is a feature inventory. Confirm its standard Elm mapping before implementation.
No Source v1 tag or metadata envelope can satisfy this delivery.

- Keep all six data forms, primary parameters, source fields/properties, and explicit constructors/accessors.
- Keep new, object initializers, member paths, assignments, with, operators, and equality.
- Keep aliases and recursive references. Do not simulate ownership, copies, object graphs, constructor execution, or synthesized equality.

## Acceptance

- Input mutation, identity observations, cycles, and recursive calls need no runtime proof.
- No hidden primary fields, generated defaults, compatibility equality helpers, or inlined bodies appear.
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
