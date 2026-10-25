# D21 — Complete acceptance

- status: planned
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D20](D20-msbuild.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Run the full source-structure suite from a clean checkout.
- Close all fixed catalogs, schema references, packages, samples, and documentation.
- Validate all 62 approved audit cases and future-delivery cases.

## Acceptance

- No pending admitted source mapping remains.
- The release states source v1 compatibility and the limits of execution semantics.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

## Historical examples

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
