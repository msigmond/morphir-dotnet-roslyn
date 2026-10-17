# Language coverage — D1

Status: D1 implementation; D2 onward pending. All rows introduced in D1. Sample owner namespace: `Example.CurrentSupported`; main container: `CurrentSupportedExample`. Every row has complete sample distribution parity against the independent Elm codec host (`fixtures/D1/expected/sample.json`). No future business-source forms are included.

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
