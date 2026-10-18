# Framework catalog — D1–D9

Baseline: .NET reference pack 10.0.0; Roslyn 5.0.0. This inventory grows in its owning deliveries and does not expand automatically with runtime/package versions.

| Exact current binding | Mapping / domain | Status / evidence |
| --- | --- | --- |
| `System.Int32` | SDK Basics.Int; full CLR input domain; no general unbounded arithmetic equivalence claim | D1/D5; comparisons, full unchecked arithmetic and MinValue through MaxValue boundaries |
| `System.Boolean` | SDK Basics.Bool; &&/|| short circuit; not, equality/inequality and eager and/or/xor | D1/D3; sample parity + behavior |
| `System.String` reference slots + known-present results | Maybe String.String for reference slots despite annotations; raw String.String only after construction/flow proof | D4; absent/present input/field/default/guard/coalescing parity |
| Compiler-synthesized positional int/bool record constructor/property | Record construction/Field, mapped from resolved source symbols | D1; positional/named/target-typed construction + field behavior |

No arbitrary framework member calls are supported through D4. SDK operators are selected from resolved built-in Roslyn operations, not framework name guesses. User record constructors/properties with bodies, clocks, external reads, mutation and unknown calls are rejected.

The fixed master catalog (numeric families, collections, dates/durations, Guid and their pure members) remains pending in D5/D6/D11/D16/D17. D6 delivers exact Decimal construction/safety; its precise mapping and member boundaries are recorded below. See the [master's fixed scope](../../Morphir_CSharp_Frontend_delivery_plan.md) for the full required inventory; this increment does not add exclusions or remove future obligations.

D2 adds machine-checkable exhaustive [fixed type inventory](framework.json), [language forms](forms.json), and [Roslyn operation inventory](operations.json). Every admitted entry has a responsible delivery and supported/pending/required-rejection state. Current Int32/literal-String subsets remain explicit above while their complete domains are pending. Each fixed framework entry records baseline version, representation/member inventory status, semantic boundaries and fixture obligations. Unknown members stay rejected; member inventory/proof closure is required in each owner delivery.

D3 introduces no new CLR type representation or arbitrary framework overload. It extends Boolean operations and known-present result origin; [D3 mapping](../mappings/D3.md) records the exact closed domain. Type/member inventory obligations in the machine catalog remain pending for their assigned deliveries.

D4 maps exact Nullable<int/bool>.HasValue, .Value, construction/conversion and GetValueOrDefault()/(T); .Value/conversion extraction requires M1, while GetValueOrDefault preserves the source API's explicit default semantics. System.String.Length requires a present receiver and counts UTF-16 code units, verified against the pinned Elm String kernel and surrogate-pair behavior. Unknown overloads remain diagnostics. Catalog ownership is clarified: D4 owns String representation/M1 and this getter; complete pure String member closure depends on later Char/array/delegate/object deliveries and remains explicitly pending at D18.4. This records dependency ownership, without excluding any pure member or claiming complete String API coverage.

D5 delivers the full fixed numeric representation/operator/conversion domains in [D5 mapping](../mappings/D5.md). The [numeric member inventory](numeric-members.json) names all 1,648 public declared overloads/properties/default constructors in the pinned reference symbols; NumericCatalogTests checks that set without regenerating it. Each member is classified individually. Type support covers no unlisted API implicitly. Decimal overloads retain D6, generic factories D10, array transport D11, increment/update D15 and object overloads D17. Remaining pure numeric API implementation/proof closure is retained under D18.4's complete-fixed-catalog obligation; ref-like/pointer forms remain master exclusions. D5's child scope is numeric domains, literals, constructors, promotions/casts/operators and their exact runtime/transport/failure behavior. This distinction preserves the master final pure-member scope and does not claim complete .NET numeric API support.

D6 represents the full CLR Decimal data domain with exact coefficient/scale/sign and SDK value, including negative zero. The [Decimal mapping](../mappings/D6.md) records exact constructors, operators, rounding/Scale/member binding, M1 and mandatory source failure proofs. [decimal-members.json](decimal-members.json) inventories all131 pinned public declarations; DecimalCatalogTests compares that set read-only. Generic, enum, buffer, object and update prerequisites remain explicit, as does D18.4 complete pure-member closure. No complete Decimal framework API claim is made.

D7 binds source calls and standard Func method references under the [call contract](../mappings/D7.md). Exact existing Decimal/classification mappings are reusable through adapters. Array data remains D11 except scoped initialized params vectors with M1/bounds proofs. External function mappings require exact metadata symbols, complete specification ABI, and explicit reviewed pure/total full-domain assertions; no opaque DLL purity inference or source-body override is allowed.

D8 adds exact baseline System.Tuple/ValueTuple constructors, Create factories and positional/Rest access in the scoped tuple data mapping, plus baseline FlagsAttribute metadata on selected source enums. Reference slots retain M1; invalid rest construction and unproved dereferences reject. Enum operators/casts reuse D5/D6 numeric proofs and do not enable arbitrary System.Enum members or framework enum declarations. Full pure API closure and dependent interfaces/arrays/object members remain assigned to later deliveries; language generic data forms are tracked in forms.json. See [D8 mapping](../mappings/D8.md) and EnumTupleTests.

The D9 source-data increment handles the compiler-resolved ValueType.Equals boundary for proved source value types using their exact field equality, including NaN-aware floating/Complex Equals and scale-insensitive Decimal equality. It preserves record-generated and resolved source value equality; unknown reference identity is rejected. This is not general System.Object/boxing or framework-member closure, which retains its catalog prerequisites. D9 passed its independent, native, rejection and cumulative gates.

D10 remains active. Ordinary selected generic source calls and pure method references do not admit generic framework APIs by name. All D10-owned generic numeric/Decimal API entries retain pending status until exact resolved conversion semantics, witnesses, safety and independent gates pass. See [D10 mapping](../mappings/D10.md).

Concrete integral generic factories have native development checks; Decimal/integral factory and Decimal integer API extensions are under validation. All D10 API entries remain pending for complete domains/witnesses and full delivery gates. See the [pinned API audit](../references/d10-numeric-api-semantics.md).

The current D10 numeric increment has concrete native matrices, NaN-aware checked bounds, Half native fallback and explicit conversion witnesses for pinned INumberBase/IFloatingPoint contracts. Eighteen complete independent development distributions are frozen. Native platform-dependent overflow, source static operators/constraints, remaining numeric domains and full cumulative/delivery gates remain open; all62 assigned API entries keep pending status.

Source static interface method constraints now have a development increment with explicit type-directed callback forwarding and effect/ownership/closure/recursion checks. Generic/private/default member and full independent parity checks are pending; static properties/operators, reference-result ownership and nonuniform self contracts remain open. This does not complete the assigned numeric contracts or their catalog entries.

Read-only source static interface properties and resolved static reference graphs now have development native checks and a twentieth independent complete distribution. Opaque callback argument aliases remain borrowed; unresolved representation/freshness and static operator/member/numeric closure gates remain open. D10 catalog states are unchanged pending the complete delivery gates.

Fieldless abstract class domains and direct source base-body calls reuse the selected-source closure/purity contract. Their runtime alternatives are exact sealed source leaves, including interface views of inherited overrides. No new framework API family is inferred; all remaining assigned numeric/member/representation obligations retain pending status.


Source base-constructor chains now participate in the same selected-source closure, purity, generic witness and ownership checks. Constructor effects and checked syntax reject even without an allocation site. No additional framework API catalog row is marked passed by this source-language increment; all62 D10 numeric API rows remain pending for full closure.


Source interface reabstraction retains exact original inherited member identities for concrete/default callback resolution. Unused overridden source defaults are still emitted and checked for effects and safety. This adds no framework mapping by name, and pending numeric API states are unchanged.


Primary base argument dependency and witness discovery includes exact generic numeric calls, while argument effects and unsafe/checked conversions reject even when no allocation site exists. The selected source domain is extended without marking any pending numeric API row passed.


Implicit source base overloads bind against the pinned compiler in a temporary equivalent initializer; only an original declaration identity is retained. Default constants preserve Decimal scale, enum values and Maybe arguments; expanded params produce an empty present list. This preserves actual C# acquisition semantics and introduces no framework API mapping or catalog status change.

The D10 inherited-property increment validates every acquired getter/setter body for effects and checked syntax. Owned source locations preserve alias reachability; mutation of borrowed receivers rejects. No additional framework API is implicitly admitted, and the62 assigned numeric API rows remain pending full delivery closure.

Instance source property contracts now validate complete resolved getters and setters. Default accessor bodies remain emitted and purity checked even when all acquired implementations override them; read/write targets retain original owner/access metadata and owned updates. The numeric factory callbacks used by generic setters still require full D10 domain/catalog closure.

Box allocation is mapped only for acquired source value alternatives with delivered mutable interface setter contracts. Receiver ownership is local, by-value inputs are copied and borrowed child references stay borrowed. This adds no general object boxing, reflection or identity API support; those fixed catalog assignments remain unchanged.
