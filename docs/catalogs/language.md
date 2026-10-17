# Language coverage — D1–D3

Status: D1–D3 passed; D4 onward pending. Existing table rows were introduced in D1. Sample owner namespace: `Example.CurrentSupported`; main container: `CurrentSupportedExample`. Every row has complete sample distribution parity against the independent Elm codec host (`fixtures/D1/expected/sample.json`). The full project-mode cumulative reference is `fixtures/D3/expected/sample.json`; the D1 standalone reference is unchanged. No future business-source forms are included.

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
| Safe Int32 addition (constant sum or zero operand) | `Addition`, `IdentityAddition`, `IdentityCast` | `eligibility`, safe archived specialization `addition`; MinValue/MaxValue identity behavior; unsafe addition rejected |
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

D3 preserves the D1 numeric contract: constants or an established zero operand only. It rebinds immutable IR variables for local/by-value parameter assignments and never mutates an input record or external state. String results are still known-present: only literals, closed source calls and initialized local/conditional combinations can produce them; general String input/data is D4. Full project-mode cumulative parity follows [the current reference pointer](../../samples/CurrentSupportedExample/reference.json); older references remain frozen.
