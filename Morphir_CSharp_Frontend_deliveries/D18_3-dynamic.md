# D18.3 — Statically resolved pure dynamic operations

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D18.2](D18_2-lazy-iterators.md), [D7](D07-calls.md), [D10](D10-generics-dispatch.md), [D17](D17-type-closure.md)
- outcome: Known pure runtime targets with C# binding semantics
- ownership: Dynamic target-set analysis; closed dispatch; binding proofs
- fixture-root: `fixtures/D18.3/`; test trait: `Delivery=D18.3`

## Implementation

- [ ] CurrentSupportedExample update: known pure dynamic members/operators/conversions, closed targets and preserved binding order
- [ ] Admitted runtime alternatives; dynamic members/operators/conversions
- [ ] Resolved target/overload/conversion behavior; evaluation order
- [ ] Canonical symbol naming + explicit dispatch + purity/failure analyses
- [ ] Unknown target set/impurity/unproved binding failure → diagnostic

## Model increment

- Resolved calls/dispatch; existing tagged/generic representations

## Fixture contract

- package: `Example`; namespace: `Example.D18_3`
- source data paths: `Example.D18_3.MinimumAge`
- member modules: `Example.D18_3.MinimumAge`, `Example.D18_3.Rules`
- constraints: Member name similarity not proof of target identity/purity

## Acceptance

- [ ] Locally constructed MinimumAge(18): Rules.adult → MinimumAge.allows; 17 → false; 18 → true
- [ ] Type Example.D18_3.MinimumAge in namespace module; member function in Example.D18_3.MinimumAge
- [ ] Known target + closed alternatives → full parity + behavior
- [ ] Unknown receiver/target; impure target; binding-failure uncertainty → rejection

## Rejections

- [ ] Unknown runtime binding; open target set; impure target; unproved binding failure

## Code examples

### C#

```csharp
namespace Example.D18_3;

public readonly record struct MinimumAge(int Minimum)
{
    public bool Allows(int age) => age >= Minimum;
}

public static class Rules
{
    public static bool Adult(int age)
    {
        dynamic rule = new MinimumAge(18);
        return rule.Allows(age);
    }
}
```

### Elm — Example/D18_3.elm

```elm
module Example.D18_3 exposing (MinimumAge)

type alias MinimumAge =
    { minimum : Int }
```

### Elm — Example/D18_3/MinimumAge.elm

```elm
module Example.D18_3.MinimumAge exposing (allows)

import Example.D18_3 exposing (MinimumAge)

allows : MinimumAge -> Int -> Bool
allows this age =
    age >= this.minimum
```

### Elm — Example/D18_3/Rules.elm

```elm
module Example.D18_3.Rules exposing (adult)

import Example.D18_3 exposing (MinimumAge)
import Example.D18_3.MinimumAge as MinimumAge

adult : Int -> Bool
adult age =
    let
        rule : MinimumAge
        rule =
            { minimum = 18 }
    in
    MinimumAge.allows rule age
```
