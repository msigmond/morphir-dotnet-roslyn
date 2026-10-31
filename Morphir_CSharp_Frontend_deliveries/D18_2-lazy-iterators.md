# D18.2 — Iterator syntax

- status: planned
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D18.1](D18_1-caught-failures.md), [D11](D11-collections.md), [D12](D12-patterns.md), [D13](D13-closures.md), [D14](D14-recursion.md), [D15](D15-loops-local-state.md)
- evidence: [exact-parity profile](../docs/deliveries/elm-parity-state.md); [remaining scope](../docs/elm-parity.md#existing-gaps)

## Prerequisite profile

Use the [D1–D9 Elm profile](../docs/deliveries/elm-parity-state.md) and all earlier exact-parity gates.
Each new mapping needs independent Elm input and complete JSON equality. Historical Source-mode passes do not count.

## Work

Scope below is a feature inventory. Confirm its standard Elm mapping before implementation.
No Source v1 tag or metadata envelope can satisfy this delivery.

- Keep iterator declarations, yield return, yield break, and source control flow.
- Keep written enumeration calls.
- Do not create lazy state machines, materialize results, or prove enumeration effects.

## Acceptance

- Iterator structure is independent of runtime enumeration.
- No hidden allocation, state transition, or disposal sequence appears.
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
namespace Example.D18_2;

public static class Rules
{
    public static System.Collections.Generic.IEnumerable<int> UpTo(int limit)
    {
        for (int index = 0; index < limit && index < 10; index++)
            yield return index;
    }

    public static int First(int limit)
    {
        foreach (int value in UpTo(limit))
            return value;
        return -1;
    }
}
```

### Elm — Example/Compat/Iterator.elm

```elm
module Example.Compat.Iterator exposing (Sequence, Step(..))

type alias Sequence state a =
    { initial : state, step : state -> Step state a }

type Step state a
    = Done
    | Yield a state
```

### Elm — Example/D18_2/Rules.elm

```elm
module Example.D18_2.Rules exposing (first, upTo)

import Example.Compat.Iterator exposing (Sequence, Step(..))

upTo : Int -> Sequence Int Int
upTo limit =
    { initial = 0
    , step =
        \index ->
            if index < limit && index < 10 then
                Yield index (index + 1)
            else
                Done
    }

first : Int -> Int
first limit =
    let
        sequence =
            upTo limit
    in
    case sequence.step sequence.initial of
        Done ->
            -1

        Yield value _ ->
            value
```
