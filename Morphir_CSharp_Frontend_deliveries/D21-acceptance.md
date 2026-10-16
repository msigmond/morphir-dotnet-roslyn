# D21 — Complete frontend acceptance

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D20](D20-msbuild.md)
- outcome: Complete agreed frontend as reproducible packages/tools
- ownership: Combined fixtures; release artifacts/docs; clean-checkout verification
- fixture-root: `fixtures/D21/`; test trait: `Delivery=D21`

## Implementation

- [ ] Combined data/collections/polymorphism/Maybe/Decimal/unchecked numerics/control flow
- [ ] Independent recursion/exception/iterator/dynamic fixtures + combined required interactions
- [ ] Checked rejection through CLI/MSBuild; fixed-version language/framework/model catalog reconciliation
- [ ] Clean checkout reproducibility; pinned dependencies; parity + behavior evidence
- [ ] Release artifacts + support contract/examples/diagnostics/mapping catalog/upgrade procedure
- [ ] CurrentSupportedExample: complete supported-language coverage; zero unmapped forms; cumulative CLI/MSBuild parity/behavior; final README/AGENTS and pointer/license audit
- [ ] External publication: separate later action; additional execution environments: separate later plan

## Model increment

- Every model case + frontend-emitted combinations validated

## Fixture contract

- package: `Example`; namespace: `Example.D21`
- source data paths: `Example.D21.Order`
- member modules: `Example.D21.Rules`
- constraints: Single combined seed not replacement for complete feature/interaction suite

## Acceptance

- [ ] Total: null → 0; eligible order units 3 → exact 0.3
- [ ] Missing units/ineligible/invalid counts → 0 contribution; first 10 orders only; units admitted 0..10; total ≤10 exact tenths
- [ ] D3 Decimal safety proof without error wrapper; all source type/member paths retained
- [ ] All master final gates + D1–D20 regression evidence; no pending admitted catalog/model case
- [ ] Clean CLI/MSBuild reproduction + packages/docs; example alone insufficient for full acceptance
- [ ] Root LICENSE/NOTICE provenance + release attribution intact; README/AGENTS commands reproducible; CLAUDE exact pointer-only body

## Rejections

- [ ] Any unresolved required scope/model/catalog entry; silent selected-code loss; failed parity/behavior gate

## Code examples

### C#

```csharp
namespace Example.D21;

public readonly record struct Order(int? Units, bool Eligible);

public static class Rules
{
    public static decimal Total(System.Collections.Generic.List<Order>? orders)
    {
        if (orders is null)
            return 0m;

        decimal total = 0m;
        int visited = 0;
        foreach (Order order in orders)
        {
            if (visited == 10)
                break;
            visited++;

            int units = order.Units ?? 0;
            if (order.Eligible && units >= 0 && units <= 10)
                total += units * 0.1m;
        }
        return total;
    }
}
```

### Elm — Example/D21.elm

```elm
module Example.D21 exposing (Order)

type alias Order =
    { units : Maybe Int, eligible : Bool }
```

### Elm — Example/D21/Rules.elm

```elm
module Example.D21.Rules exposing (total)

import Example.D21 as Data
import Morphir.SDK.Decimal as Decimal exposing (Decimal)

total : Maybe (List Data.Order) -> Decimal
total possibleOrders =
    case possibleOrders of
        Nothing ->
            Decimal.zero

        Just orders ->
            let
                addOrder : Data.Order -> Decimal -> Decimal
                addOrder order runningTotal =
                    let
                        units =
                            case order.units of
                                Nothing ->
                                    0

                                Just value ->
                                    value
                    in
                    if order.eligible && units >= 0 && units <= 10 then
                        Decimal.add runningTotal
                            (Decimal.mul (Decimal.fromInt units) (Decimal.tenth 1))
                    else
                        runningTotal
            in
            List.foldl addOrder Decimal.zero (List.take 10 orders)
```
