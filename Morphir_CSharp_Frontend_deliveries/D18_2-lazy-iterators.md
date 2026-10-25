# D18.2 — Iterator syntax

- status: planned
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D18.1](D18_1-caught-failures.md), [D11](D11-collections.md), [D12](D12-patterns.md), [D13](D13-closures.md), [D14](D14-recursion.md), [D15](D15-loops-local-state.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Keep iterator declarations, yield return, yield break, and source control flow.
- Keep written enumeration calls.
- Do not create lazy state machines, materialize results, or prove enumeration effects.

## Acceptance

- Iterator structure is independent of runtime enumeration.
- No hidden allocation, state transition, or disposal sequence appears.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

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
