# D10 — Generics, interfaces, inheritance

- status: active
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D9](D09-user-data.md)
- outcome: Generic business values + closed pure polymorphic dispatch
- ownership: Generic representations; closure analysis; dispatch/cast lowering
- fixture-root: `fixtures/D10/`; test trait: `Delivery=D10`
- active evidence: [D10 state](../docs/deliveries/D10-state.md); ordinary generics and closed value/reference interface graph increments are under validation; the complete delivery gates below remain open

## Implementation

- [ ] CurrentSupportedExample update: generic constraints/variance, interfaces/base/override dispatch, casts/type tests, closed runtime alternatives
- [ ] Generic types/methods; constraints; variance; interfaces; abstract/base types; inheritance/overrides
- [ ] Ordinary generic parameters → IR variables; closed runtime variants → constructors + explicit dispatch
- [ ] Resolved specialization only where required; unclosed runtime implementation sets rejected
- [ ] Casts/type tests/dispatch: resolved implementations + representation; closure completeness diagnostics
- [ ] Generic slots retain argument representation, including Maybe where absence possible

## Model increment

- Type variables; custom constructors; remaining required specifications

## Fixture contract

- package: `Example`; namespace: `Example.D10`
- source data paths: `Example.D10.Box`, `Example.D10.IRule`, `Example.D10.MaximumAge`, `Example.D10.MinimumAge`
- member modules: `Example.D10.IRule`, `Example.D10.MaximumAge`, `Example.D10.MinimumAge`, `Example.D10.Rules`
- constraints: Data module and same-named member module distinct; receiver references namespace-defined type

## Acceptance

- [ ] Box<T> → Box t; type paths Example.D10.Box/IRule/MinimumAge/MaximumAge
- [ ] MinimumAge/MaximumAge data in Example.D10; allows functions in separate corresponding member modules
- [ ] IRule representation: MinimumAgeRule | MaximumAgeRule; IRule.allows dispatch in Example.D10.IRule
- [ ] Sample admitted implementation set: MinimumAge + MaximumAge; nullable rule → explicit Maybe
- [ ] Generic wrapper + constrained function + interface/base dispatch → parity and behavior
- [ ] Unknown implementation set, ambiguous/incomplete closure → rejection

## Rejections

- [ ] Open/unknown runtime implementation set; impure target; unsafe cast/dereference

## Code examples

### C#

```csharp
namespace Example.D10;

public readonly record struct Box<T>(T Value);

public interface IRule
{
    bool Allows(int age);
}

public readonly record struct MinimumAge(int Minimum) : IRule
{
    public bool Allows(int age) => age >= Minimum;
}

public readonly record struct MaximumAge(int Maximum) : IRule
{
    public bool Allows(int age) => age <= Maximum;
}

public static class Rules
{
    public static Box<T> Wrap<T>(T value) => new(value);

    public static bool Test(IRule? rule, int age)
    {
        if (rule is null)
            return false;
        return rule.Allows(age);
    }
}
```

### Elm — Example/D10.elm

```elm
module Example.D10 exposing (Box, IRule(..), MaximumAge, MinimumAge)

type alias Box t =
    { value : t }

type alias MinimumAge =
    { minimum : Int }

type alias MaximumAge =
    { maximum : Int }

type IRule
    = MinimumAgeRule MinimumAge
    | MaximumAgeRule MaximumAge
```

### Elm — Example/D10/MinimumAge.elm

```elm
module Example.D10.MinimumAge exposing (allows)

import Example.D10 exposing (MinimumAge)

allows : MinimumAge -> Int -> Bool
allows this age =
    age >= this.minimum
```

### Elm — Example/D10/MaximumAge.elm

```elm
module Example.D10.MaximumAge exposing (allows)

import Example.D10 exposing (MaximumAge)

allows : MaximumAge -> Int -> Bool
allows this age =
    age <= this.maximum
```

### Elm — Example/D10/IRule.elm

```elm
module Example.D10.IRule exposing (allows)

import Example.D10 exposing (IRule(..))
import Example.D10.MaximumAge as MaximumAge
import Example.D10.MinimumAge as MinimumAge

allows : IRule -> Int -> Bool
allows this age =
    case this of
        MinimumAgeRule rule ->
            MinimumAge.allows rule age

        MaximumAgeRule rule ->
            MaximumAge.allows rule age
```

### Elm — Example/D10/Rules.elm

```elm
module Example.D10.Rules exposing (test, wrap)

import Example.D10 exposing (Box, IRule)
import Example.D10.IRule as IRule

wrap : t -> Box t
wrap value =
    { value = value }

test : Maybe IRule -> Int -> Bool
test possibleRule age =
    case possibleRule of
        Nothing ->
            False

        Just rule ->
            IRule.allows rule age
```
