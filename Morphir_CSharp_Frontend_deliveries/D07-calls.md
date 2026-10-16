# D7 — Resolved calls, overloads, parameters

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D2](D02-project-inputs.md), [D3](D03-blocks-locals.md), [D4](D04-maybe.md), [D5](D05-numerics.md), [D6](D06-decimal.md)
- outcome: Pure composition with resolved overloads and source argument semantics
- ownership: Call graph; symbol naming; invocation lowering; purity summaries
- fixture-root: `fixtures/D7/`; test trait: `Delivery=D7`

## Implementation

- [ ] CurrentSupportedExample update: static/instance calls, overloads, named/optional/params arguments, local functions, method references
- [ ] Static + pure instance calls; explicit receiver; overloads; optional/named arguments; params; local functions; method references
- [ ] Resolved symbols; source argument evaluation order; optional defaults from bound declaration
- [ ] Canonical callable signature suffix: generic arity, parameter/ref kinds; nested declarations; collision diagnostics
- [ ] Purity across reachable constructors/getters/operators/helpers; explicit external mapping contract

## Model increment

- Apply/Reference/Function; named inputs; helper definitions

## Fixture contract

- package: `Example`; namespace: `Example.D7`
- source data paths: —
- member modules: `Example.D7.Rules`
- constraints: Overload suffixes: disambiguation within member module; no substitute for owner identity

## Acceptance

- [ ] Named arguments → intended parameters and evaluation order; declaration-derived optional default
- [ ] Rules overloads: distinct private definitions; private dependencies retained
- [ ] Illustrative meetsBool/meetsIntInt suffixes → final canonical signature-derived names in paired fixtures
- [ ] Instance receiver type paths + helper calls resolve intended owner; normalized collision diagnostics include both locations

## Rejections

- [ ] Unknown purity/external contract; unresolved/colliding target; hidden impure getter/operator/constructor

## Code examples

### C#

```csharp
namespace Example.D7;

public static class Rules
{
    private static bool Meets(int age, int minimum = 18) =>
        age >= minimum;

    private static bool Meets(bool approved) => approved;

    public static bool Eligible(int age) =>
        Meets(minimum: 18, age: age);

    public static bool Approved(bool approved) => Meets(approved);

    public static bool DefaultEligible(int age) => Meets(age);
}
```

### Elm — Example/D7/Rules.elm

```elm
module Example.D7.Rules exposing (approved, defaultEligible, eligible)

meetsIntInt : Int -> Int -> Bool
meetsIntInt age minimum =
    age >= minimum

meetsBool : Bool -> Bool
meetsBool approved =
    approved

eligible : Int -> Bool
eligible age =
    meetsIntInt age 18

approved : Bool -> Bool
approved value =
    meetsBool value

defaultEligible : Int -> Bool
defaultEligible age =
    meetsIntInt age 18
```
