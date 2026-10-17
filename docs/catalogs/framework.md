# Framework catalog — D1–D5

Baseline: .NET reference pack 10.0.0; Roslyn 5.0.0. This inventory grows in its owning deliveries and does not expand automatically with runtime/package versions.

| Exact current binding | Mapping / domain | Status / evidence |
| --- | --- | --- |
| `System.Int32` | SDK Basics.Int; full CLR input domain; no general unbounded arithmetic equivalence claim | D1/D5; comparisons, full unchecked arithmetic and MinValue through MaxValue boundaries |
| `System.Boolean` | SDK Basics.Bool; &&/|| short circuit; not, equality/inequality and eager and/or/xor | D1/D3; sample parity + behavior |
| `System.String` reference slots + known-present results | Maybe String.String for reference slots despite annotations; raw String.String only after construction/flow proof | D4; absent/present input/field/default/guard/coalescing parity |
| Compiler-synthesized positional int/bool record constructor/property | Record construction/Field, mapped from resolved source symbols | D1; positional/named/target-typed construction + field behavior |

No arbitrary framework member calls are supported through D4. SDK operators are selected from resolved built-in Roslyn operations, not framework name guesses. User record constructors/properties with bodies, clocks, external reads, mutation and unknown calls are rejected.

The fixed master catalog (numeric families, collections, dates/durations, Guid and their pure members) remains pending in D5/D6/D11/D16/D17. Decimal construction/safety is D6; no decimal source operation is currently represented. See the [master's fixed scope](../../Morphir_CSharp_Frontend_delivery_plan.md) for the full required inventory; this increment does not add exclusions or remove future obligations.

D2 adds machine-checkable exhaustive [fixed type inventory](framework.json), [language forms](forms.json), and [Roslyn operation inventory](operations.json). Every admitted entry has a responsible delivery and supported/pending/required-rejection state. Current Int32/literal-String subsets remain explicit above while their complete domains are pending. Each fixed framework entry records baseline version, representation/member inventory status, semantic boundaries and fixture obligations. Unknown members stay rejected; member inventory/proof closure is required in each owner delivery.

D3 introduces no new CLR type representation or arbitrary framework overload. It extends Boolean operations and known-present result origin; [D3 mapping](../mappings/D3.md) records the exact closed domain. Type/member inventory obligations in the machine catalog remain pending for their assigned deliveries.

D4 maps exact Nullable<int/bool>.HasValue, .Value, construction/conversion and GetValueOrDefault()/(T); .Value/conversion extraction requires M1, while GetValueOrDefault preserves the source API's explicit default semantics. System.String.Length requires a present receiver and counts UTF-16 code units, verified against the pinned Elm String kernel and surrogate-pair behavior. Unknown overloads remain diagnostics. Catalog ownership is clarified: D4 owns String representation/M1 and this getter; complete pure String member closure depends on later Char/array/delegate/object deliveries and remains explicitly pending at D18.4. This records dependency ownership, without excluding any pure member or claiming complete String API coverage.

D5 delivers the full fixed numeric representation/operator/conversion domains in [D5 mapping](../mappings/D5.md). The [numeric member inventory](numeric-members.json) names all 1,648 public declared overloads/properties/default constructors in the pinned reference symbols; NumericCatalogTests checks that set without regenerating it. Each member is classified individually. Type support covers no unlisted API implicitly. Decimal overloads retain D6, generic factories D10, array transport D11, increment/update D15 and object overloads D17. Remaining pure numeric API implementation/proof closure is retained under D18.4's complete-fixed-catalog obligation; ref-like/pointer forms remain master exclusions. D5's child scope is numeric domains, literals, constructors, promotions/casts/operators and their exact runtime/transport/failure behavior. This distinction preserves the master final pure-member scope and does not claim complete .NET numeric API support.
