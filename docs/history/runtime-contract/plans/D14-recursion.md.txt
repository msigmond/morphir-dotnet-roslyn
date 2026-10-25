# D14 — Self and mutual recursion

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D7](D07-calls.md), [D13](D13-closures.md)
- outcome: Self/mutually recursive pure business functions
- ownership: Call SCCs; recursive references/local bindings; reference-mode handling
- fixture-root: `fixtures/D14/`; test trait: `Delivery=D14`

## Implementation

- [ ] CurrentSupportedExample update: self/mutual/local recursion; recorded reference mode or complete independent codec policy
- [ ] Strongly connected call groups; dependency closure
- [ ] Local recursion → LetRecursion; top-level recursion → correct owner references
- [ ] No termination-proof requirement; no artificial recursion/iteration limit
- [ ] All-public sample reference: make --fallback-cli; default 2.100.0 ValueCycleDetected limitation recorded
- [ ] Combined/private recursive fixtures: independent complete Elm IR/codec reference where source modes cannot preserve contract

## Model increment

- LetRecursion; recursive top-level Reference

## Fixture contract

- package: `Example`; namespace: `Example.D14`
- source data paths: —
- member modules: `Example.D14.Rules`
- constraints: Only displayed all-public fixture uses fallback; ordinary/private fixtures retain default or independent codec policy

## Acceptance

- [ ] Even(4) → true; Odd(4) → false; nonpositive base cases source-defined
- [ ] Self-recursion + mutual recursion + recursive locals: full parity; bounded runtime cases
- [ ] Fallback provenance recorded; no fallback private-helper Public promotion or JSON patch

## Rejections

- [ ] Unresolved recursive reference; arbitrary recursion cutoff; loss of private metadata

## Code examples

### C#

```csharp
namespace Example.D14;

public static class Rules
{
    public static bool Even(int value) =>
        value <= 0 || Odd(value - 1);

    public static bool Odd(int value) =>
        value > 0 && Even(value - 1);
}
```

### Elm — Example/D14/Rules.elm

```elm
module Example.D14.Rules exposing (even, odd)

even : Int -> Bool
even value =
    if value <= 0 then
        True
    else
        odd (value - 1)

odd : Int -> Bool
odd value =
    if value > 0 then
        even (value - 1)
    else
        False
```
