# D15 — Loops, local mutation, ref/out/in

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D11](D11-collections.md), [D14](D14-recursion.md)
- outcome: Imperative pure calculations → functional state transitions
- ownership: Loop/CFG state lowering; alias-sensitive ref calls
- fixture-root: `fixtures/D15/`; test trait: `Delivery=D15`

## Implementation

- [ ] CurrentSupportedExample update: loop forms, break/continue/early return, local mutation, owned ref/out/in operations
- [ ] for/foreach/while/do; break/continue; assignments/increments; early returns; nested loops
- [ ] Evaluation/iteration order; functional state + recursion; local aliases
- [ ] Ordinary ref/out/in parameters; locally owned writes; explicit functional return/state representation
- [ ] Writes through input/external references → rejection

## Model increment

- Functional bindings/recursion; explicit state tuples; Unit

## Fixture contract

- package: `Example`; namespace: `Example.D15`
- source data paths: —
- member modules: `Example.D15.Rules`
- constraints: Ordinary ref/out/in support separate from ref-like data exclusion

## Acceptance

- [ ] CountUpTo: 4 → 4; 100 → 10; negative → 0
- [ ] FlipTwice → false; private Rules.toggle → (Unit, updated Bool); public return Bool
- [ ] Accumulation/early exit/nested loops; out/in/ref alias-sensitive operations; parity + C# boundaries
- [ ] Input/external write via ref/helper/new wrapper → rejection

## Rejections

- [ ] Input/global/captured mutation; ref-like data forms; alias behavior loss

## Code examples

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
