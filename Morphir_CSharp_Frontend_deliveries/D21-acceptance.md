# D21 — Complete acceptance

- status: planned
- contract: [exact Elm parity](../docs/elm-parity.md)
- prerequisites: [D20](D20-msbuild.md)
- evidence: [current acceptance gaps](../docs/elm-parity.md#existing-gaps); earlier migration evidence is historical

## Work

Scope below is a feature inventory. Confirm its standard Elm mapping before implementation.
No Source v1 tag or metadata envelope can satisfy this delivery.

- Run the complete exact Elm parity suite from a clean checkout.
- Close all fixed catalogs, schema references, packages, samples, and documentation.
- Validate all 62 approved audit cases and future-delivery cases.

## Acceptance

- No pending admitted source mapping remains.
- The release reports standard Morphir compatibility and exact-parity coverage for every feature.
- Every feature needs paired C#/Elm inputs and full JSON equality against `morphir-elm make`.
- Ignore only whitespace/object-key order. No package, name, type, or operation normalization.
- Ask the user if the Elm counterpart is unclear. No unrequested output or feature is permitted.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context. New acceptance references must come from `morphir-elm make`.

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
