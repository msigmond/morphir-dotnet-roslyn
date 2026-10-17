# Framework catalog — D1–D3

Baseline: .NET reference pack 10.0.0; Roslyn 5.0.0. This inventory grows in its owning deliveries and does not expand automatically with runtime/package versions.

| Exact current binding | Mapping / domain | Status / evidence |
| --- | --- | --- |
| `System.Int32` | SDK Basics.Int; full CLR input domain; no general unbounded arithmetic equivalence claim | D1; comparisons/zero-addition MinValue through MaxValue; unsafe addition rejected |
| `System.Boolean` | SDK Basics.Bool; &&/|| short circuit; not, equality/inequality and eager and/or/xor | D1/D3; sample parity + behavior |
| `System.String` closed known-present results | SDK String.String, proved present by literal construction and closed local/branch/source-call origin | D1/D3; no string field/input/null extraction admitted |
| Compiler-synthesized positional int/bool record constructor/property | Record construction/Field, mapped from resolved source symbols | D1; positional/named/target-typed construction + field behavior |

No arbitrary framework member calls are supported through D3. SDK operators are selected from resolved built-in Roslyn operations, not framework name guesses. User record constructors/properties with bodies, clocks, external reads, mutation and unknown calls are rejected.

The fixed master catalog (numeric families, collections, dates/durations, Guid and their pure members) remains pending in D5/D6/D11/D16/D17. Decimal construction/safety is D6; no decimal source operation is currently represented. See the [master's fixed scope](../../Morphir_CSharp_Frontend_delivery_plan.md) for the full required inventory; this increment does not add exclusions or remove future obligations.

D2 adds machine-checkable exhaustive [fixed type inventory](framework.json), [language forms](forms.json), and [Roslyn operation inventory](operations.json). Every admitted entry has a responsible delivery and supported/pending/required-rejection state. Current Int32/literal-String subsets remain explicit above while their complete domains are pending. Each fixed framework entry records baseline version, representation/member inventory status, semantic boundaries and fixture obligations. Unknown members stay rejected; member inventory/proof closure is required in each owner delivery.

D3 introduces no new CLR type representation or arbitrary framework overload. It extends Boolean operations and known-present result origin; [D3 mapping](../mappings/D3.md) records the exact closed domain. Type/member inventory obligations in the machine catalog remain pending for their assigned deliveries.
