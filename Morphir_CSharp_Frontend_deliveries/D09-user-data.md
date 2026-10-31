# D9 — Pure data and member functions

- status: passed under the approved Elm profile
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D7](D07-calls.md), [D8](D08-enums-tuples.md)
- evidence: [current gates](../docs/deliveries/elm-parity-state.md); [approved mappings](../docs/reviews/d1-d9-elm-mapping-decisions.md)

## Approved scope

Class/struct/record data use record aliases. Primary parameters become same-named fields. Constructors that assign every stored field once become record-building functions. Getters/operators become named functions called at use sites. Record equality and with-updates use standard operations.

## Boundary

Mutation, identity/cycles, recursive aliases, constructor chaining, extra constructor statements, reads of incomplete this, implicit field defaults, setters, and general object/member initializers remain rejected.
Unmapped forms fail at the source. They produce no partial output.
No Source v1 output or custom source tag is accepted.

## Acceptance

- Every feature needs paired C#/Elm inputs and full JSON equality against `morphir-elm make`.
- Ignore only whitespace/object-key order. No package, name, type, or operation normalization.
- Keep the fixed SDK/reference/Roslyn versions, declared types, namespace selection, and atomic output.
- Run restore, build, the full suite, the D1–D9 catalog gate, and repository checks.
- Use the [case registry](../fixtures/elm-parity/cases.json) and executable rejection inputs.

The examples below are historical. They do not replace current compiler parity evidence.

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
