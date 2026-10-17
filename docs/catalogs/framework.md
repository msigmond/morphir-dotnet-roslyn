# Framework catalog — D1

Baseline: .NET reference pack 10.0.0; Roslyn 5.0.0. This inventory grows in its owning deliveries and does not expand automatically with runtime/package versions.

| Exact current binding | Mapping / domain | Status / evidence |
| --- | --- | --- |
| `System.Int32` | SDK Basics.Int; full CLR input domain; no general unbounded arithmetic equivalence claim | D1; comparisons/zero-addition MinValue through MaxValue; unsafe addition rejected |
| `System.Boolean` | SDK Basics.Bool; &&/|| short circuit | D1; sample parity + behavior |
| `System.String` literal only | SDK String.String, proved present by literal construction | D1; no string field/input/null extraction admitted |
| Compiler-synthesized positional int/bool record constructor/property | Record construction/Field, mapped from resolved source symbols | D1; positional/named/target-typed construction + field behavior |

No arbitrary framework member calls are supported in D1. SDK operators are selected from resolved built-in Roslyn operations, not framework name guesses. User record constructors/properties with bodies, clocks, external reads, mutation and unknown calls are rejected.

The fixed master catalog (numeric families, collections, dates/durations, Guid and their pure members) remains pending in D5/D6/D11/D16/D17. Decimal construction/safety is D6; no decimal source operation is currently represented. See the [master's fixed scope](../../Morphir_CSharp_Frontend_delivery_plan.md) for the full required inventory; this increment does not add exclusions or remove future obligations.
