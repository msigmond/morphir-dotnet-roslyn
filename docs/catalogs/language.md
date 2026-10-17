# Language coverage — D1–D7

Status: D1–D7 passed; D8 onward pending. Existing table rows were introduced in D1. Sample owner namespace: `Example.CurrentSupported`; main container: `CurrentSupportedExample`. Every row has complete sample distribution parity against the independent Elm codec host (`fixtures/D1/expected/sample.json`). The full project-mode cumulative reference is identified by samples/CurrentSupportedExample/reference.json; the D1 standalone reference is unchanged. No future business-source forms are included.

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
