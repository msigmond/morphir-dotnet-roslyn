# D18.4 — Admitted feature closure and exclusions

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D18.1](D18_1-caught-failures.md), [D18.2](D18_2-lazy-iterators.md), [D18.3](D18_3-dynamic.md)
- outcome: No unsupported admitted C# 14 pure feature
- ownership: Final bound-operation/CFG audit; remaining admitted forms; rejection matrix
- fixture-root: `fixtures/D18.4/`; test trait: `Delivery=D18.4`

## Implementation

- [ ] CurrentSupportedExample update: every remaining admitted language form; coverage-map closure; rejected forms isolated from sample
- [ ] Audit all bound operations + relevant control-flow forms against fixed catalog
- [ ] Remaining admitted forms: actual lowering + runnable positive/negative/boundary fixtures
- [ ] Exclusions: async/await/async iteration; pointer/ref-like/stackalloc; LINQ; checked; identity; cyclic graphs; input/external mutation/effects
- [ ] M1/D3/general failure contract reconciliation; no arbitrary rejection to claim admitted-feature completion

## Model increment

- Remaining required lowering cases; no excluded-source enablement

## Fixture contract

- package: `Example`; namespace: `Example.D18_4`
- source data paths: —
- member modules: `Example.D18_4.Rules`
- constraints: Rejected block isolated from positive project; D18 umbrella = D18.1–D18.4 only

## Acceptance

- [ ] Adult regression unchanged; 17 → false; 18 → true
- [ ] Each rejected source declaration: own source diagnostic; whole conversion failure; no Elm emitted
- [ ] Separate negative fixtures: LINQ, pointers/ref-like, identity, cycles, external mutation
- [ ] No pending admitted form; required M1/D3 rejections distinguished from implementation gaps

## Rejections

- [ ] Displayed CheckedIncrement/Delayed/UnhandledDivide; every confirmed exclusion
- [ ] Silent omission; arbitrary rejection of otherwise admitted pure form

## Code examples

### C# — accepted regression

```csharp
namespace Example.D18_4;

public static class Rules
{
    public static bool Adult(int age) => age >= 18;
}
```

### Elm — Example/D18_4/Rules.elm

```elm
module Example.D18_4.Rules exposing (adult)

adult : Int -> Bool
adult age =
    age >= 18
```

### C# — rejected cases

```csharp
// Rejected input. Do not include this block in the accepted fixture project.
namespace Example.Rejected;

public static class Rules
{
    public static int CheckedIncrement(int value) => checked(value + 1);

    public static async System.Threading.Tasks.Task<int> Delayed(
        System.Threading.Tasks.Task<int> value) => await value;

    public static int UnhandledDivide(int value, int divisor) => value / divisor;
}
```
