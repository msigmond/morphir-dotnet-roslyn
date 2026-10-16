# D18.2 — Pure lazy iterators

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D18.1](D18_1-caught-failures.md), [D11](D11-collections.md), [D12](D12-patterns.md), [D13](D13-closures.md), [D14](D14-recursion.md), [D15](D15-loops-local-state.md)
- outcome: Lazy yields + early consumer termination
- ownership: Functional iterator state/step/disposal; deferred failure/ownership
- fixture-root: `fixtures/D18.2/`; test trait: `Delivery=D18.2`

## Implementation

- [ ] CurrentSupportedExample update: yield/break, deferred/repeated/nested consumption, early disposal/finally and handled deferred failures
- [ ] yield return/yield break; argument evaluation; captured/local state; deferred execution; element order
- [ ] Repeated/nested enumeration; early stop/disposal; finally cleanup
- [ ] Failure handling at actual execution/consumption point; no moved handlers
- [ ] Alias/ownership tracking across iterator production/consumption; no external effects or async enumeration
- [ ] Valid nonrecursive generic state encoding; sample Sequence/Step extended for disposal/failure semantics

## Model increment

- Functional state + deferred step; explicit disposal transition as required

## Fixture contract

- package: `Example`; namespace: `Example.D18_2`
- source data paths: —
- member modules: `Example.D18_2.Rules`
- constraints: Simple sample has no cleanup; general fixtures require explicit disposal transition

## Acceptance

- [ ] First(5) → 0 without producing later elements; First(0) → -1
- [ ] Step only on consumer request; returned state used by next step; each enumeration starts fresh
- [ ] Sample bounds: ≤10 elements; arithmetic safe; Rules.upTo/first retained; generated Iterator state separate
- [ ] Deferred execution/repeated enumeration/nesting/early disposal/finally fixtures → parity + C# behavior
- [ ] Failure before later yield: correct handler at consumption or rejection if escape possible
- [ ] Eager-list replacement fails acceptance

## Rejections

- [ ] Asynchronous iteration; external effects; unhandled deferred failure
- [ ] Eager materialization or omitted disposal/cleanup

## Code examples

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
