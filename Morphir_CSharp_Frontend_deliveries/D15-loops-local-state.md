# D15 — Loops, compound updates, and references

- status: planned
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D11](D11-collections.md), [D14](D14-recursion.md)
- evidence: [exact-parity profile](../docs/deliveries/elm-parity-state.md); [remaining scope](../docs/elm-parity.md#existing-gaps)

## Work

Scope below is a feature inventory. Confirm its standard Elm mapping before implementation.
No Source v1 tag or metadata envelope can satisfy this delivery.

- Keep loop kind, initializer, condition, iterator, body, labels, and branches.
- Keep increment, compound assignment, ref/out/in, ref locals, and ref returns.
- Do not convert loops to recursion or simulate aliases, snapshots, state tuples, or iteration.

## Acceptance

- Every written update and control-flow boundary remains visible.
- No ownership, termination, or runtime bounds proof is required.
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
