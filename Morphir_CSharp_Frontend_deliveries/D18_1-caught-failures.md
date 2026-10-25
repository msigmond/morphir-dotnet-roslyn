# D18.1 — Exception syntax

- status: planned
- contract: [source structure](../docs/source-structure.md)
- prerequisites: [D3](D03-blocks-locals.md), [D4](D04-maybe.md), [D5](D05-numerics.md), [D6](D06-decimal.md), [D7](D07-calls.md), [D8](D08-enums-tuples.md), [D9](D09-user-data.md), [D10](D10-generics-dispatch.md), [D11](D11-collections.md), [D12](D12-patterns.md), [D13](D13-closures.md), [D14](D14-recursion.md), [D15](D15-loops-local-state.md), [D16](D16-framework-data.md), [D17](D17-type-closure.md)
- evidence: [migration state](../docs/deliveries/source-structure-state.md)

## Work

- Keep try/catch/finally, filters, throw, and rethrow in source order.
- Keep explicit exception type and binding information.
- Do not convert failures into Result, Maybe, hidden defaults, or simulated exception flow.

## Acceptance

- Caught and uncaught failure syntax stays visible.
- Throwing behavior is not an admission gate.
- Add independent source/IR assertions and cumulative sample coverage.
- Run affected delivery tests, catalog checks, and repository checks.
- Keep toolchain pins, selected-source errors, source locations, and atomic output.

The C# examples below are feature inputs. The Elm examples show the former runtime mapping.
They are historical context and are not the expected source v1 output.

## Historical examples

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
