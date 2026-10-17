# D3 — Blocks, locals, decisions

- status: passed
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D2](D02-project-inputs.md)
- outcome: Business rule with intermediate state and multiple returns
- ownership: Operation/CFG lowering; local state; branch fixtures
- fixture-root: `fixtures/D3/`; test trait: `Delivery=D3`

## Implementation

- [x] CurrentSupportedExample update: blocks, locals/constants, reassignment, branches/conditional expressions, returns
- [x] Block bodies; locals/constants; allowed reassignment; returns; conditional expressions; if/else
- [x] Boolean/short-circuit lowering; numeric operations restricted to delivered contracts
- [x] IOperation + control-flow analysis; scopes, evaluation order, definite assignment

## Model increment

- LetDefinition; IfThenElse; additional required bindings

## Fixture contract

- package: `Example`; namespace: `Example.D3`
- source data paths: —
- member modules: `Example.D3.Rules`
- constraints: Later numeric support additive; no premature generic arithmetic claim

## Acceptance

- [x] Category: inactive → 0; active age <18 → 1; active adult → 2
- [x] Branch coverage + locals/reassignment parity; operand/branch changes reflected in IR

## Rejections

- [x] Unproved numeric semantics; unassigned read; input/external mutation; effects

## Code examples

### C#

```csharp
namespace Example.D3;

public static class Rules
{
    public static int Category(int age, bool active)
    {
        int category = 0;
        if (active)
        {
            category = 1;
            if (age >= 18)
                category = 2;
        }
        return category;
    }
}
```

### Elm — Example/D3/Rules.elm

```elm
module Example.D3.Rules exposing (category)

category : Int -> Bool -> Int
category age active =
    let
        initialCategory =
            0
    in
    if active then
        let
            activeCategory =
                1
        in
        if age >= 18 then
            2
        else
            activeCategory
    else
        initialCategory
```
