# Language coverage — D1–D7

Status: D1–D9 passed; D10 onward pending. Existing table rows were introduced in D1. Sample owner namespace: `Example.CurrentSupported`; main container: `CurrentSupportedExample`. Every row has complete sample distribution parity against the independent Elm codec host (`fixtures/D1/expected/sample.json`). The full project-mode cumulative reference is identified by samples/CurrentSupportedExample/reference.json; the D1 standalone reference is unchanged. No future business-source forms are included.

| Supported form | Cumulative example symbol/location | Additional parity / behavior evidence |
| --- | --- | --- |
| Positional readonly record struct; int/bool fields | `Customer`, `Unused`, `CurrentSupportedExample.Nested` | `eligibility`, `private`, `selection`, `outside`; record constructor boundary checks |
| Top-level and nested source owners; static members on data | `Customer.DataConstant`, `CurrentSupportedExample.Nested`, `CurrentSupportedExample.Tools.Echo`, `CurrentSupportedExampleTools.Number` | Nested/partial owner tests; no repeated data module/type |
| Static/partial containers, expression bodies, by-value parameters | Both `CurrentSupportedExample` declarations; `Tools.Echo` | Partial declarations combined; emitted C# behavior |
| Public/internal/private declarations, including unused data/functions | `Customer`, `Unused`, `Hidden`, `HiddenFlag` | `private` full codec parity; public specification filtering |
| Canonical overload identity | `Echo(int)`, `Echo(bool)` | Sample full parity; typed identity behavior at Int32/Bool boundaries |
| Primitive int/bool literals, constant negative Int32 | `Addition`, `Named`, `Always`, `Negative`, `HiddenFlag` | Parsed JSON literal-kind tests; compiled C#/IR behavior |
| Known-present string literal; summary documentation | `EligibleLabel` | `eligibility`; known-present literal only, docs included in sample parity |
| Parameter references and readonly data property reads | `Eligible`, `Tools.Echo` | `eligibility`, `outside`; ages MinValue, -1, 0, 17, 18, 19, MaxValue |
| Record construction: explicit type, target-typed and named arguments | `Create`, `TargetTyped`, `Named` | `eligibility`; C#/IR fields and parameter order checked |
| Int32 `>=`, `>`, `<`, `<=`, `==`, `!=` | `Eligible`, `ActiveOrAdult`, `Below`, `AtMost`, `Exactly`, `Different` | All comparison boundaries; all fields/typed attributes in full parity |
| Boolean `&&`, `||`, left-to-right short circuit | `Eligible`, `ActiveOrAdult` | Interpreter deliberately omits unused RHS field to prove short circuit |
| Safe Int32 addition (constant sum or zero operand) | `Addition`, `IdentityAddition`, `IdentityCast` | `eligibility`, safe archived specialization `addition`; MinValue/MaxValue identity behavior; general unchecked addition delivered in D5 |
| Parentheses, identity cast, explicit unchecked context, escaped identifier | `IdentityAddition`, `IdentityCast(int @checked)` | Sample parity; checked syntax/semantic rejection and comments/string/@checked regressions |

Selection is an adapter contract rather than a new business expression: complete segment/descendant/overlap/private/outside-source fixtures are in `FrontendTests`. Compilation uses all explicit files, including unselected files for symbol binding, but conversion never implicitly selects referenced namespaces.

D1 does not admit locals/aliases/mutation, nullable data or Decimal arithmetic. Their positive proof obligations remain D3–D6/D15; negative D1 fixtures reject these forms without fallback behavior. No language or framework scope has been removed from later deliveries.

## D2 additions

| Form / contract | Cumulative example | Complete parity / behavior evidence |
| --- | --- | --- |
| Real project and source reference, descendant selection | `CurrentSupportedExample.csproj` → `CurrentSupportedShared` | D2 sample, project/input/selection tests |
| Multifile and generated partial declarations | `ProjectMode.cs`, generated `Rules.Generated.cs` on CurrentSupportedExample | D2 sample; GeneratedThreshold emitted unused |
| Actual generator AdditionalFiles/configuration | `threshold.txt` + CompilerVisibleProperty Threshold; incremental generator | Changed/missing/throwing generator tests; input hashes |
| Private same-module call | `ProjectEligible` → `ProjectAdult` | D2 sample/calls; age boundaries |
| Legal internal cross-project qualified call, Private IR | `ProjectAdult` → Shared.AgeRule.Adult, friend assembly | D2 sample/internal; private-inclusive lookup, public specs |
| Distinct source containers with same method names | Original Eligibility owners + project/shared owners | D2 calls/project/internal distribution equality |

[forms.json](forms.json) assigns every fixed language/purity/safety form through D18.4. [operations.json](operations.json) inventories every named Roslyn 5.0.0 OperationKind including aliases/non-C# markers; subset domains and pending owners are explicit. [framework.json](framework.json) inventories the exact fixed primitive/framework families; pending representations/member/proof work blocks final release. Catalog presence does not imply implementation.

## D3 additions

| Form / contract | Cumulative example | Complete parity / behavior evidence |
| --- | --- | --- |
| Block scopes, locals/constants, later initialization | `Scoped`, `InitLater`, `KnownLabel` in Blocks.cs | D3 sample complete codec parity; compiled C# boundary behavior; compiler unassigned/scope rejection |
| Local/by-value parameter reassignment, value copies | `Category`, `Reassign`, `ValueCopy` | D3 ordinary Category and sample parity; MinValue/MaxValue and record copy behavior |
| if/else, early/multiple returns, typed conditional | `Category`, `Early`, `ConstantBranch` | All active/age branches; changed assignment changes IR and result; CFG return paths |
| Boolean not/equality/inequality, eager and/or/xor | `Boolean` | All four Boolean inputs; typed SDK mapping |
| Nested assignment expressions, operand snapshots | `NestedAssignment`, `AssignmentOrder` | D3 sample parity; emitted C# comparison; later zero cannot erase earlier unknown numeric input |
| Short circuit/conditional assignment expressions | `ShortAssignment`, `ConditionalAssignment` | Every Boolean combination; branch-specific local state |
| Source-order call argument assignments | `ArgumentAssignment`, private `FirstArgument` | Complete private definition/call parity; original first argument retained |

Historically, D3 preserved the D1 numeric contract: constants or an established zero operand only. It rebinds immutable IR variables for local/by-value parameter assignments and never mutates an input record or external state. String results are still known-present: only literals, closed source calls and initialized local/conditional combinations can produce them; general String input/data is D4. Full project-mode cumulative parity follows [the current reference pointer](../../samples/CurrentSupportedExample/reference.json); older references remain frozen.

## D4 additions

| Form / contract | Cumulative example in Presence.cs | Evidence |
| --- | --- | --- |
| Nullable value/reference fields and annotated reference slot | `OptionalCustomer`, `AnnotatedReference`, `EchoReference` | Full D4 sample and ordinary seed parity; null/empty/present inputs |
| HasValue/.Value, null tests and dominance | `MissingAdult`, `GuardedAnnotation`, `GuardCopy` | Boundaries and invalidated/saved guard negatives; immutable value copies |
| Null/default/empty record construction and present construction proof | `NullResult`, `DefaultOptional`, `DefaultRecord`, `ConstructOptional`, `ConstructedAge`, `NullableLocal` | Full typed constructors/record fields; default-null despite annotation; `!` alone rejected |
| Coalescing/conditional access, UTF-16 Length | `RegionOrUnknown`, `RegionLength`, `ReferenceCall` | Null/empty/UK/surrogate pair; source fallback only |
| Nullable comparisons, arithmetic, three-valued Boolean logic | `NullableEqual`, `NullableGreater`, `NullableAddZero`, `NullableAnd/Or/Xor/Not` | All Boolean? pairs; nullable Int32 extrema against emitted C# |
| Coalescing assignment and source-state order | `InitializeMissing`, `CoalesceState` | Branch-specific rebinding, later read and fallback evaluation |
| Exact Nullable.GetValueOrDefault member | `DefaultFromMember` | Explicit source member/default semantics; zero/explicit fallback variants in tests |

The D4 null-slot representation is reusable by later array/reference data owners; D11 still owns array construction/indexing and D9 owns reference-class data. These future forms are not enabled prematurely. String member families requiring Char/arrays/delegates/object and the remaining pure-member closure remain catalog obligations through D18.4; D4 claims M1 representation and the exact Length getter only. No type/member scope is removed.

## D5 additions

| Form / contract | Cumulative example in Numeric.cs | Evidence |
| --- | --- | --- |
| Every primitive integer/Char width, signedness, native64, Int128/UInt128/BigInteger | NumericPromotion, NumericByte/SByte/Short/UShort/Char/UInt/Long/ULong/Native/UNative/Int128/UInt128/BigInteger | Full independent numeric/sample distributions; CLR boundary checks and exact Elm/JS BigInt checks |
| Width wrapping, bitwise and shifts, including BigInteger logical shift | NumericNext, NumericBits, NumericBigLogical | Int32 extrema, negative/masked counts, full signed/unsigned widths and BigInteger storage boundaries |
| Single/Double/Half and Complex arithmetic | NumericSingle/Double/Half/Complex | Float/Complex tests: precision, nonfinite values, signed zero, scalar overload order |
| Numeric casts, nullable conversion, exact wide literals/constructors, sizeof | NumericIntegerToDouble, NumericDoubleToByte, NumericNullable, NumericLargeLong, NumericMaximum128, NumericConstruct128, NumericSize | Wide limb transport; rounding/truncation/saturation and nullable unary/conversion tests |
| Dominating failure proofs and IEEE predicates | NumericDivide, NumericNarrow, NumericFinite, NumericComplexInteger | NumericSafetyTests and stale guard negatives; no partial artifact on rejection |
| BigInteger binary64 truncation | NumericBigDouble | Pinned runtime comparison at >53-bit and signed 128-bit boundaries |

D5 supersedes the historical safe-addition-only limitation with exact unchecked arithmetic. Earlier references retain their specialized structures, while formerly unproved general addition is now tested positively. Decimal and later call/data/state forms retain their assigned delivery gates. Full framework API closure remains explicit in numeric-members.json and is distinct from delivered language operators.

## D6 additions

Decimal.cs adds exact0.1/1.2300 construction, the guarded0..100 Price result, rounding, observable scale, signs/comparison and integral conversion. DecimalTests checks nullable slots/operators, local updates, source-call bounds, wide conversions, rounding/float boundaries, stale proofs and source-located safety failures. Complete independent source/sample/model codec references live in fixtures/D6. Decimal language support retains exact member/precondition boundaries in decimal-members.json and mappings/D6.md; complete framework closure remains explicit.

## D7 additions

Calls.cs adds canonical private overloads, declaration defaults, named evaluation order, nullable/empty/valued params packs, guarded element reads, explicit record receivers, noncapturing local definitions and method references with receiver snapshots. CallTests/ExternalCallTests verify source/cumulative/full external-spec distributions and emitted C#/IR behavior. Unknown callable effects and hidden/unmapped bodies reject. Params-vector support does not enable general D11 arrays, and generated adapters do not enable D13 source lambdas/captures.

## D8 additions

EnumsTuples.cs adds numeric SampleAccess aliases/flags/unnamed values and named tuple results, deconstruction and M1 reference tuples. EnumTupleTests exercises all eight enum underlying domains, scoped member ownership, nullable flags/casts/equality, value/reference constructors, long/nested tuples, copying/local updates, destination conversions, defaults, source evaluation order and unsafe access rejection. Full independent ordinary/codec/cumulative references are under fixtures/D8. See [D8 mapping](../mappings/D8.md). General user data/dispatch/generics/collections retain their prerequisite gates.

D9 passed. Its current increment covers non-generic class/struct/record data layouts and owners, primary/explicit construction and initialization, pure getters/setters/init/indexers, field-backed C#14 properties, local alias-preserving updates across mixed value/reference locations, source operators/conversions and exact value equality. Independent complete Counter/data-form parity and native boundary/rejection checks pass; complete cumulative/final D9 gates passed. See [D9 mapping](../mappings/D9.md) and [execution evidence](../deliveries/D9-state.md). D9 form/operation catalog owners are supported with exact evidence.

D10 is active, not passed. Its scoped generic storage/witnesses, inherited value/reference interfaces, owned reference graph results, uniform generic interface layouts/members and default bodies, constrained views and guarded type-test/cast increments are recorded in the [D10 mapping](../mappings/D10.md); the full generic/variance/inheritance/cast/closure and cumulative gates remain pending. D10 form catalog entries stay pending until their complete obligations pass.

Active D10 numeric checks cover concrete factories and explicit generic conversion witnesses, safe checked floating bounds and stored predicate snapshots under validation. Their independent fixtures are FloatingFactories.cs/GenericNumericFactories.cs; native tests are NumericFactoryTests/FloatingFactoryProofTests/GenericNumericFactoryTests. The cumulative source remains the passed D9 baseline until the remaining D10 inheritance/specialization/variance/static-member and numeric gates are complete.

Source static method/property constraints, private/default/inherited targets and resolved result graph continuations have active D10 development checks in StaticInterfaceTests. Complete static-interfaces/static-properties distributions are independently authored and frozen. Generic numeric callback scopes across owned results are awaiting the final increment regression; variance, nonuniform specialization, reabstraction, inheritance and remaining numeric/member closure still block D10 completion.

The D10 class increment covers fieldless abstract bases with sealed leaves, override/default/inherited methods, direct base-body calls and guarded runtime views. AbstractClassTests verifies native leaf storage, dynamic self calls, object result aliases and rejected incomplete base storage/construction/open domains. Independent complete AbstractRules.cs/BaseBodies.cs references preserve source owners and private helpers. The full inheritance/variance/specialization and cumulative obligations remain pending.

Default inherited fields have a D10 native/ownership increment: leaf constructor assignment, base reads/writes, exact zero/null defaults and returned child aliases. Full inheritance storage/initializers/constructors and remaining obligations stay pending. The new InheritedFields.cs whole-model reference is independently authored and frozen.


The active D10 base-construction increment adds explicit/implicit base and this-constructor chains, source-ordered inherited field initialization and virtual self calls during construction. Generic witnesses propagate through actual constructor targets; owned returned children remain live. Hidden slots, base properties and concrete nonsealed layouts remain pending. D10 is still active; see the current evidence and mapping.


Interface reabstraction now resolves concrete or restored most-specific source targets, retaining generic type/method arguments and nullable/alias representations. Static methods and read-only static properties use their resolved callbacks. Every selected default body remains emitted and checked even if all alternatives override it. The25th complete independent reference is under final increment validation; full D10 remains pending.


Primary base argument invocations now use their resolved targets and source argument order, including local primary parameter rebinding before virtual base-body observations. Inherited primary captures retain generic Maybe arguments and child aliases. Constructor numeric requirements propagate through argument calls. Native and complete26th-reference checks passed; full increment regression remains pending.


The current D10 storage increment distinguishes hidden fields and primary captures by the declaring source owner, including different field types and generic Maybe/alias payloads. Scoped numeric proofs survive repeated reads of one version and do not cross hidden slots or writes. Implicit base optional/params calls use the compiler-selected accessible constructor and original typed defaults, including partial primary declarations. The27th/28th complete references and85 affected cases pass; final increment/full D10 gates remain pending.

The current D10 property increment covers nonvirtual inherited automatic, field-backed and custom getters/setters, hidden property storage and owned field/property coalescing with lazy RHS evaluation. Virtual/abstract/interface properties remain pending. Unknown generic runtime type equality rejects MCS3103 when arguments could coincide, pending witnesses or necessary specialization; it is never folded to an invented false result. D10 remains active.

The current polymorphic property increment adds virtual/abstract class and default/reabstracted interface getters/setters, exact inherited override selection, automatic/positional accessor functions, direct base calls and indexed argument ordering. Nullable custom/virtual/indexed property coalescing preserves lazy RHS, receiver identity, struct copies and the actual assignment result. These development checks do not complete boxed value mutation, nonsealed/record inheritance, specialization/variance or D10 final gates.

The concrete nonsealed D10 increment distinguishes base, intermediate and leaf runtime alternatives, including base allocations, hidden/nonvirtual private owner reads, direct base bodies and virtual self calls during construction. Primary/optional constructor binding, generic Maybe slots, interface/cast views and recursive owned graph proofs retain their exact source semantics. Record inheritance and final D10 gates remain pending.

Owned mutable interface boxes now distinguish CLR boxing identity from value copies without admitting identity observations. Mapped setter calls share a box across interface views; separate boxing and unboxing retain copies, including nullable/generic slots and returned boxes. Borrowed reachability remains immutable and cyclic graphs reject. Unresolved generic view ownership and the remaining D10 final gates remain pending.
