# D15 — Loops, compound updates, and references

- status: planned
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D11](D11-collections.md), [D14](D14-recursion.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Keep loop kind, initializer, condition, iterator, body, labels, and branches.
- Keep increment, compound assignment, ref/out/in, ref locals, and ref returns.
- Do not convert loops to recursion or simulate aliases, snapshots, state tuples, or iteration.

## Acceptance

- Every written update and control-flow boundary remains visible.
- No ownership, termination, or runtime bounds proof is required.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

## Historical examples

### C#

```csharp
namespace Example.D15;

public static class Rules
{
    public static int CountUpTo(int limit)
    {
        int count = 0;
        for (int index = 0; index < limit && index < 10; index++)
            count++;
        return count;
    }

    private static void Toggle(ref bool value) => value = !value;

    public static bool FlipTwice()
    {
        bool value = false;
        Toggle(ref value);
        Toggle(ref value);
        return value;
    }
}
```

### Elm — Example/D15/Rules.elm

```elm
module Example.D15.Rules exposing (countUpTo, flipTwice)

countUpTo : Int -> Int
countUpTo limit =
    let
        loop index count =
            if index < limit && index < 10 then
                loop (index + 1) (count + 1)
            else
                count
    in
    loop 0 0

toggle : Bool -> ( (), Bool )
toggle value =
    ( (), not value )

flipTwice : Bool
flipTwice =
    let
        ( _, afterFirst ) =
            toggle False

        ( _, afterSecond ) =
            toggle afterFirst
    in
    afterSecond
```
