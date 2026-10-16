# D13 — Lambdas, delegates, captures

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D7](D07-calls.md), [D10](D10-generics-dispatch.md)
- outcome: Pure higher-order function + immutable capture
- ownership: Higher-order lowering; closure representation/effects
- fixture-root: `fixtures/D13/`; test trait: `Delivery=D13`

## Implementation

- [ ] CurrentSupportedExample update: lambdas/anonymous methods, delegate data/method groups/invocation, immutable capture
- [ ] Lambdas/anonymous methods; delegate data; method groups; invocation; captured immutable values
- [ ] Capture lifetime + evaluation behavior; locally owned effects only with proof
- [ ] Captured external mutation/persistent externally visible closure state → purity rejection

## Model increment

- Lambda; Function types; valid-IR closure representation

## Fixture contract

- package: `Example`; namespace: `Example.D13`
- source data paths: —
- member modules: `Example.D13.Rules`
- constraints: Immutable captured values admitted

## Acceptance

- [ ] AtLeast(18): returned function rejects 17, accepts 18
- [ ] Function parameter + returned closure + invocation fixtures; captured value lifetime; parity
- [ ] External captured mutation → rejection; wholly local effects → ownership evidence

## Rejections

- [ ] External captured mutation; observable persistent closure state; unknown delegate purity

## Code examples

### C#

```csharp
namespace Example.D13;

public static class Rules
{
    public static System.Func<int, bool> AtLeast(int minimum) =>
        age => age >= minimum;
}
```

### Elm — Example/D13/Rules.elm

```elm
module Example.D13.Rules exposing (atLeast)

atLeast : Int -> (Int -> Bool)
atLeast minimum =
    \age -> age >= minimum
```
