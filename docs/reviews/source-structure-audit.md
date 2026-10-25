# C# source structure: change review

Status: approved on 2026-10-24. Base: `e083653`. Scope: the former D1–D9 runtime contract.
This audit is the approved change request. See [implementation and validation](../deliveries/source-structure-state.md).

## Proposed rule

Represent the source declarations and operations. Do not simulate CLR execution.
Use `Maybe T` only for an explicit nullable type, such as `string?` or `int?`.
Do not require proof that an operation is safe at runtime.
Keep explicit checks in the source. Do not add checks or remove branches.

“Proof” means a frontend check of possible runtime values.
“Expansion” means extra operations that implement one source operation.
The future column records the approved change. Current implementation status is in the migration evidence.

## The four reported cases

| Source | Current behavior | Future behavior |
| --- | --- | --- |
| `Loan(string SecurityId, decimal Quantity, decimal Price)` | `SecurityId` becomes `Maybe String`. | Use `String`. Use `Maybe String` for `string?` only. |
| `loan.Quantity * loan.Price` | Requires proof that `loan` is present. Otherwise, reports `MCS2501`. | Keep two member reads and one multiplication. Require no null guard. |
| The same decimal expression | Requires proof of result bounds. Otherwise, reports `MCS2702`. | Keep one decimal multiplication. Require no range guard. |
| `obj.IntOne * obj.IntTwo` | Adds integer conversion, multiplication, width wrapping, and conversion back. | Keep two member reads and one integer multiplication. |

Use a direct SDK operation where its type fits. Select a simple mapping for other operations.
The operation need not reproduce CLR overflow, rounding, or failure behavior.

## N. Null and presence

Sources: [type mapping](../history/runtime-contract/frontend/Frontend.cs),
[presence](../history/runtime-contract/frontend/Frontend.Presence.cs),
[data types](../history/runtime-contract/frontend/Frontend.Data.cs),
[tuples](../history/runtime-contract/frontend/Frontend.Tuples.cs),
[function values](../history/runtime-contract/frontend/Frontend.MethodReferences.cs).

| ID | Case | Current behavior | Future behavior |
| --- | --- | --- | --- |
| N1 | Reference slots | Strings, classes, reference tuples, delegates, and params arrays can receive `Maybe` without `?`. | Apply the explicit nullable type rule to parameters, returns, fields, properties, and nested type arguments. |
| N2 | Method result types | Output can depend on the translated body and called method outputs. Some present string/function results omit `Maybe`. | Derive the output from the declared type. Do not change it from value facts. |
| N3 | Dereference and extraction | Member reads, instance calls, string length, delegate calls, `.Value`, and nullable casts can require presence proof. `!` supplies none. | Keep the written access or cast. Require no presence proof. Retain explicit `!` if source detail is retained. |
| N4 | Null tests and guards | Null tests and `HasValue` become matches. Guards bind extracted values and change later access. | Keep the explicit test and branch. Do not add proof bindings to later operations. |
| N5 | `??`, `??=`, `?.`, defaults | Expands to matches, constructors, assignments, and default values. Known values can remove cases. | Keep each written operation and fallback. Do not compute implicit defaults. |
| N6 | Nullable operators | Arithmetic, comparisons, Boolean operators, and casts expand to null cases and `Just`/`Nothing` operations. | Keep one typed operation. Retain the nullable operand and result types. |
| N7 | Presence adaptation | Assignments, returns, calls, and branch joins can insert `Just` or widen values to `Maybe`. | Follow declared nullability. Do not infer nullable declarations from possible runtime values. |

N1 also applies when nullable analysis is disabled. Inspect explicit source `?`; compiler flow state alone is insufficient.
Explicit `null` must remain visible. Do not change `string` to `string?` to accommodate it.
This case needs a type-model decision; see the review choices below.

## A. Integer, floating, and complex numbers

Sources: [integers](../history/runtime-contract/frontend/Frontend.Numerics.cs),
[conversions](../history/runtime-contract/frontend/Frontend.NumericConversions.cs),
[floating operations](../history/runtime-contract/frontend/Frontend.Floating.cs),
[integer proofs](../history/runtime-contract/frontend/Frontend.NumericProofs.cs),
[floating proofs](../history/runtime-contract/frontend/Frontend.FloatingProofs.cs).
Helper bodies: [integer library](../../eng/numeric-compat/src/Example/Compat/Integers.elm),
[floating library](../../eng/numeric-compat/src/Example/Compat/Floating.elm).

| ID | Case | Current behavior | Future behavior |
| --- | --- | --- | --- |
| A1 | Integer operators | Most arithmetic and bit operations use `fromSmall`, helper calls, `wrap`, and `toSmall`. Some safe Int32 additions remain simple. | Map each source operator to one simple operation. Do not change structure based on proof facts. |
| A2 | Wide integer data | Int64, native integers, 128-bit integers, and BigInteger use sign and digit records. Literals become digit lists. | Use named numeric types and literals. Do not expose storage digits. |
| A3 | Shifts | Adds count masking and width/sign rules. BigInteger unsigned shift simulates storage width. | Keep one shift and the written count. |
| A4 | Integer division and remainder | Requires nonzero proof (`MCS2601`). Signed cases also exclude minimum-value division by minus one (`MCS2602`). | Keep `/` or `%`. Require neither proof. |
| A5 | Numeric casts and constructors | Adds wrapping, saturation, truncation, or rounding. BigInteger bounds, finite values, and zero imaginary parts require proofs (`MCS2603/2604/2606`). | Keep the cast or constructor and its target type. Require no value proof. |
| A6 | Implicit promotions | Compiler-inserted conversions can add operations that the source does not contain. Identity and widening conversions can disappear. | Keep explicit casts. Represent implicit type resolution without runtime conversion steps. |
| A7 | Float and Half arithmetic | Inserts precision rounding. Half operations can round through Single first. | Keep one arithmetic operation. Preserve the declared numeric type where required. |
| A8 | Special floating operations | NaN, infinities, and negative zero use helpers. Remainder and `IsFinite` expand into algorithms or tests. | Keep the named constant, operator, or method call. |
| A9 | Complex operations | Constructors become component records. Operators and equality expand into component calculations or helper algorithms. | Keep the Complex type, constructor, operator, and member access. |
| A10 | Numeric type distinctions | Several integer types share `Int`. Half, Single, and Double share `Float`. Char uses an integer representation. | Select simple target types. Retain source type identity where the target type loses it. |

## D. Decimal

Sources: [decimal mapping](../history/runtime-contract/frontend/Frontend.Decimal.cs),
[members and conversions](../history/runtime-contract/frontend/Frontend.DecimalMembers.cs),
[proofs](../history/runtime-contract/frontend/Frontend.DecimalProofs.cs),
[helper bodies](../../eng/decimal-compat/src/Example/Compat/Decimal.elm).

| ID | Case | Current behavior | Future behavior |
| --- | --- | --- | --- |
| D1 | Decimal type and literals | Uses coefficient, scale, sign, and SDK-value records. Constants use string parsing and a present-case match. | Use a simple Decimal type and decimal literal. Do not add parsing or storage fields. |
| D2 | Decimal arithmetic | Uses compatibility algorithms and an extra epsilon argument. Requires result bounds (`MCS2702`). | Use one operation per source operator. Require no result bounds. |
| D3 | Decimal division and remainder | Requires nonzero proof (`MCS2701`). Division also requires result bounds. | Keep `/` or `%`. Require no proof. |
| D4 | Decimal conversions | Requires coefficient or target bounds (`MCS2703`). Decimal-to-float conversion expands coefficient parts and scale branches. | Keep one cast or constructor. Require no range proof. |
| D5 | Floating-to-decimal input | Rejects binary-floating construction (`MCS2704`), even when C# permits it. | Keep the explicit cast or constructor. |
| D6 | Scale and rounding digits | Requires constructor scale and `Round` digits in `0..28` (`MCS2706`). | Keep the arguments as written. Require no bounds. |
| D7 | Decimal members | `Abs`, `Sign`, comparisons, `Min`, `Max`, rounding, and other mapped members become calculations or field reads. | Keep each named call or property access. Keep explicit rounding calls. |

## C. Calls and function values

Sources: [calls](../history/runtime-contract/frontend/Frontend.cs),
[local functions](../history/runtime-contract/frontend/Frontend.Callables.cs),
[method references](../history/runtime-contract/frontend/Frontend.MethodReferences.cs),
[params](../history/runtime-contract/frontend/Frontend.Params.cs),
[external contracts](../history/runtime-contract/frontend/Frontend.External.cs),
[data calls](../history/runtime-contract/frontend/Frontend.DataCalls.cs).

| ID | Case | Current behavior | Future behavior |
| --- | --- | --- | --- |
| C1 | Named and optional arguments | Orders inputs by parameter position. Inserts omitted defaults. Extra bindings preserve source evaluation order. | Retain written arguments, names, and order. Keep defaults on parameter declarations. |
| C2 | Expanded params calls | Creates a nullable list, including an empty list for omitted arguments. | Keep the written argument list and the params declaration. Do not insert an array allocation. |
| C3 | Params length and indexing | Requires presence and bounds (`MCS2501/2808`). Indexing becomes list drop, head, and extraction. | Keep length access and indexing. Require no runtime proof. |
| C4 | Method references | Builds lambda adapters and receiver snapshots. Zero-argument Func uses a Unit argument. | Keep the method reference, receiver, and declared function signature. |
| C5 | Delegate invocation | Requires a known pure function origin (`MCS2804`). Captures and mutable receiver references face further restrictions (`MCS2802`). | Keep invocation and capture structure. Require no proof of effects or receiver lifetime. |
| C6 | Local functions | Moves local definitions to separate modules. Rejects captures (`MCS2802`). | Retain local scope and captured names. Do not create capture storage operations. |
| C7 | External calls | Requires a mapped catalog member or an exact pure, total specification contract (`MCS2202/2809`). | Keep the resolved call and signature. Require a representation, not a runtime safety assertion. |
| C8 | Data-returning calls | Expands selected factory, getter, and operator bodies at the caller to track allocation and returned aliases. | Keep one call. Keep the definition separately. |
| C9 | Recursive calls | Rejects call cycles (`MCS2203`), including constructor and accessor cycles. | Keep resolved recursive references. Require no termination proof. |
| C10 | Instance calls | Converts the receiver to an explicit input. Several paths require presence proof. | Retain receiver association. A Classic function mapping needs only the minimum receiver argument. |

## O. Data and state

Sources: [data and layouts](../history/runtime-contract/frontend/Frontend.Data.cs),
[graphs and ownership](../history/runtime-contract/frontend/Frontend.DataGraphs.cs),
[constructors](../history/runtime-contract/frontend/Frontend.DataConstructors.cs),
[initializers](../history/runtime-contract/frontend/Frontend.DataInitializers.cs),
[object initializers](../history/runtime-contract/frontend/Frontend.DataObjectInitializers.cs),
[primary parameters](../history/runtime-contract/frontend/Frontend.DataPrimary.cs),
[getters](../history/runtime-contract/frontend/Frontend.DataProperties.cs),
[setters](../history/runtime-contract/frontend/Frontend.DataSetters.cs),
[locations](../history/runtime-contract/frontend/Frontend.DataLocations.cs),
[records](../history/runtime-contract/frontend/Frontend.DataRecords.cs),
[equality](../history/runtime-contract/frontend/Frontend.DataEquality.cs),
[operators](../history/runtime-contract/frontend/Frontend.DataOperators.cs).

| ID | Case | Current behavior | Future behavior |
| --- | --- | --- | --- |
| O1 | Data declarations | Classes, structs, and records become record aliases or custom wrappers. Fields and stored properties share layout slots. | Retain source data form, members, and modifiers where required. Do not expose CLR storage layout. |
| O2 | Hidden primary storage | Adds storage for captured primary-constructor parameters. Positional records receive property initialization operations. | Keep the primary parameters and their source references. Do not add hidden fields or assignments. |
| O3 | Object construction | Creates zero/null/default storage, runs initializers and constructor chains, and expands bodies at construction sites. | Keep `new`, its arguments, and written initializers. Keep explicit constructor bodies and chains separately. |
| O4 | Getter, setter, init, indexer | Reads become fields or generated getter calls. Setters become receiver-returning functions and can expand at the caller. | Retain access syntax and explicit accessor definitions. Do not execute accessor bodies during translation. |
| O5 | Aliases, copies, and updates | Tracks owned cells and versions. Rebuilds records and nested tuples to preserve runtime alias observations and struct copies. | Keep aliases, assignments, and member paths as written. Do not simulate a heap. |
| O6 | Input and external mutation | Rejects writes without local ownership (`MCS3002`). Unknown reference results can remove later mutation permission. | Represent explicit writes. Require no ownership or escape proof. |
| O7 | Recursive data and object cycles | Adds custom payload wrappers and match extraction. Rejects cyclic object graphs (`MCS3007`). | Keep type references and source assignments. Do not construct or test the runtime graph. |
| O8 | `with` and record copies | Expands shallow-copy state, copy constructors, and initializers into record updates. | Keep the `with` expression. Retain explicit copy constructors separately. |
| O9 | Record and struct equality | Expands fields, null cases, NaN handling, decimal comparison, and recursive equality helpers. | Keep one equality operation or `Equals` call. Do not generate compiler-supplied equality bodies. |
| O10 | Reference identity | Rejects reference `==`, `ReferenceEquals`, some nested equality, and some record identity shortcuts (`MCS3003`). | Keep the resolved operator or call. Require no identity proof. |
| O11 | User operators | Changes operators to function calls. Lifted and conditional operators add null cases and true/false calls. | Retain written operators and explicit operator definitions. Do not add hidden operator calls. |

## T. Enums and tuples

Sources: [enums](../history/runtime-contract/frontend/Frontend.Enums.cs),
[tuples](../history/runtime-contract/frontend/Frontend.Tuples.cs).

| ID | Case | Current behavior | Future behavior |
| --- | --- | --- | --- |
| T1 | Enum values and operators | Uses numeric wrapper constructors. Casts and flags operations unwrap, calculate, wrap widths, and rebuild values. | Keep the enum, member references, underlying type, casts, and operators. |
| T2 | Enum initializers | Keeps member definitions, but uses compiler-computed values. Alias and flags expressions lose their written form. | Keep each explicit initializer expression. Distinguish omitted initializers. |
| T3 | Tuple data and labels | Uses positional tuples. Labels and some Tuple/ValueTuple distinctions disappear. Reference tuples receive Maybe. | Retain tuple family, labels, and explicit nullability. |
| T4 | Tuple reads and construction | Reads become pattern matches. Factories become tuple values and hidden Rest construction. Rest also needs validity/presence checks (`MCS2906`). | Keep member access, constructors, and factory calls. Require no runtime Rest proof. |
| T5 | Tuple conversion and equality | Rebuilds converted elements. Equality becomes element reads, conversions, comparisons, and short-circuit branches (`MCS2907` for unmapped cases). | Keep one cast or equality operation. Do not expand element behavior. |
| T6 | Deconstruction and tuple writes | Adds snapshots, extraction patterns, conversions, bindings, and whole-tuple reconstruction. | Keep deconstruction, discards, and written element assignments. |

## S. Source statements and compile-time changes

Sources: [blocks](../history/runtime-contract/frontend/Frontend.Blocks.cs),
[expressions](../history/runtime-contract/frontend/Frontend.Expressions.cs),
[constants](../history/runtime-contract/frontend/Frontend.DataConstants.cs),
[main translation](../history/runtime-contract/frontend/Frontend.cs).

| ID | Case | Current behavior | Future behavior |
| --- | --- | --- | --- |
| S1 | Assignments and returns | Reassignments become fresh immutable bindings. Returns become expressions. Following statements can be copied into both branches. | Retain statement order, assignment targets, returns, and branch boundaries. |
| S2 | Short circuit | `&&` and `||` become conditional expressions. Proof-sensitive cases add further branches. | Retain the source operator. Do not expand runtime branch steps. |
| S3 | Constant simplification | Known conditions can remove branches. Constant declarations and some arithmetic, unary operations, and casts become computed literals. | Keep initializer expressions, operators, casts, and all written branches. |
| S4 | Default and size expressions | `default`, parameterless numeric construction, and supported `sizeof` become computed values or records. | Keep the written expression and type operand. |
| S5 | Empty and uninitialized statements | Empty statements and declarations without initializers can disappear. Discarded expressions receive generated bindings. | Retain declarations and explicit expression statements. Do not invent discard variables. |
| S6 | Syntax details | Parentheses, identity casts, `!`, and checked/unchecked context lack dedicated output nodes. Names and dictionary order are normalized. | Preserve grouping and explicit syntax where needed. Keep source names/order as metadata if Classic normalizes them. |

## G. Rejection rules and output constraints

Sources: [external rules](../history/runtime-contract/frontend/Frontend.External.cs),
[source input](../../src/Morphir.Frontend.CSharp/SourceCompilation.cs),
[project input](../../src/Morphir.Frontend.CSharp.ProjectSystem/ProjectLoader.cs),
[CLI](../../src/Morphir.Frontend.CSharp.Cli/Program.cs),
[model](../../src/Morphir.IR.Classic/Model.cs),
[codec](../../src/Morphir.IR.Json/Codec.cs),
[form inventory](../catalogs/forms.json),
[framework inventory](../catalogs/framework.md).

| ID | Case | Current behavior | Future behavior |
| --- | --- | --- | --- |
| G1 | Checked operations | Rejects explicit checked syntax (`MCS2001`) and checked semantic operations (`MCS2004`), including unreachable source. | Retain explicit context or operator detail. Add no overflow algorithm. Compiler options must not add source steps. |
| G2 | Purity and effects | Rejects unknown/effectful members, mutable global storage, and unsupported call bodies. Safety checks also inspect unused and unreachable code. | Keep written operations when their structure is supported. Require no purity or runtime safety proof. |
| G3 | Pending language forms | Generics, inheritance, dispatch, general collections, closures, loops, ref forms, exceptions, async, and other forms remain restricted. | Define structural nodes or mappings before support. Removing proofs alone does not implement these forms. |
| G4 | Compatibility modules | Numeric or decimal use inserts complete helper modules, including definitions unrelated to the written operation. | Emit source definitions and required type/operation references. Do not insert CLR simulation libraries. |
| G5 | Classic representation limits | The current model has no direct assignment, return, cast, index, constructor-call, or nullable-access node. | Select a model extension or a documented minimal mapping. Do not hide source loss in runtime expansion. |

Keep compiler syntax and binding checks. Keep source locations, namespace selection, symbol resolution, and collision checks.
Keep source project and generator acquisition. Generated source is an explicit input; CLR-synthesized runtime machinery is different.
Keep complete output checks and atomic writes. The codec adds no runtime behavior.
These input and output rules do not require a change for the four reported cases.

## Approved implementation choices

1. Use an explicit source v1 extension for source forms absent from Classic. Keep simple Classic/SDK operations where suitable.
2. Retain data forms, widths, labels, modifiers, names, grouping, and declaration order in source metadata.
3. Keep explicit null as a typed Source Null operation. Do not widen a nonnullable declaration.
4. Use one Source operation when no suitable SDK mapping exists. Do not emit runtime helper algorithms.

The source v1 envelope is distinct from standard Classic v3. See the [source contract](../source-structure.md).
Whitespace and comments are not round-trip data. Selected declaration structure and token spelling are retained.

## Audit evidence and later validation

Reviewed all frontend partial files, input adapters, model/codec paths, compatibility factories and source mappings.
Cross-checked the D1–D9 mappings, coverage catalogs, and integration checks.
The original audit was static. Implementation and final validation are recorded in [migration evidence](../deliveries/source-structure-state.md).

The master, all 24 delivery plans, catalogs, mappings, sample, README, AGENTS, and tests now use the source contract.
All four reported cases and every case ID are checked. Historical references remain frozen.
New references remain separate reviewed artifacts; validation does not refresh them.
