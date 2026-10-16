# D18.1 — Caught failures and exception flow

- status: planned
- required-context: [master](../Morphir_CSharp_Frontend_delivery_plan.md) + this file + prerequisite evidence
- prerequisites: [D3](D03-blocks-locals.md), [D4](D04-maybe.md), [D5](D05-numerics.md), [D6](D06-decimal.md), [D7](D07-calls.md), [D8](D08-enums-tuples.md), [D9](D09-user-data.md), [D10](D10-generics-dispatch.md), [D11](D11-collections.md), [D12](D12-patterns.md), [D13](D13-closures.md), [D14](D14-recursion.md), [D15](D15-loops-local-state.md), [D16](D16-framework-data.md), [D17](D17-type-closure.md)
- outcome: Source-defined recovery with ordinary business return type
- ownership: Failure summaries; throw/handler/filter/rethrow/finally lowering
- fixture-root: `fixtures/D18.1/`; test trait: `Delivery=D18.1`

## Implementation

- [ ] CurrentSupportedExample update: throw/try/catch/filter/rethrow/finally, explicit recovery, interprocedural handling; M1/D3 unchanged
- [ ] Pure throw/try/catch/filter/rethrow/finally; handler type/order; nested handling; local updates
- [ ] Interprocedural failure propagation; helper failure to proven handler; exposed public API nonfailure proof
- [ ] Finally on normal/early return/failure; source evaluation order
- [ ] Caught collection/parse/cast/match failures; no M1/Decimal/checked bypass
- [ ] Complete paired reference distribution including internal failure helpers; sample branch form only semantic seed

## Model increment

- Private failure/control-flow data and helpers; preserved access wrappers

## Fixture contract

- package: `Example`; namespace: `Example.D18_1`
- source data paths: —
- member modules: `Example.D18_1.Rules`
- constraints: General lowering includes explicit failure/control-flow state; displayed branch-only reference not full general implementation

## Acceptance

- [ ] DivideOrZero: 7/2 → 3; -7/2 → -3; divisor 0 → 0; int.MinValue/-1 → 0
- [ ] Throw/rethrow; filter selection/order; nested handlers; finally-on-return/failure
- [ ] Private helper failure caught by caller → success; public selected failing function without own handler → rejection despite caller catch
- [ ] Normal + handled failure paths → parity and C# behavior; removed handlers → rejection unless proven safe

## Rejections

- [ ] Possible failure escaping public business function; impure handler
- [ ] Unsafe M1 access/Decimal operation/checked syntax even within catch

## Code examples

### C#

```csharp
namespace Example.D18_1;

public static class Rules
{
    public static int DivideOrZero(int numerator, int denominator)
    {
        try
        {
            return numerator / denominator;
        }
        catch (System.DivideByZeroException)
        {
            return 0;
        }
        catch (System.OverflowException)
        {
            return 0;
        }
    }
}
```

### Elm — Example/D18_1/Rules.elm

```elm
module Example.D18_1.Rules exposing (divideOrZero)

divideOrZero : Int -> Int -> Int
divideOrZero numerator denominator =
    if denominator == 0 then
        0
    else if numerator == -2147483648 && denominator == -1 then
        0
    else
        numerator // denominator
```
