> Source v1 was never requested. This document records legacy code, not the delivery contract. Use [exact Elm parity](elm-parity.md). Do not extend this format.

# Unrequested Source v1 implementation

Source-structure audit: [audit](reviews/source-structure-audit.md). Validation: [state](deliveries/source-structure-state.md).
The audit approved source-structure rules. It did not request this custom output format.

## Output

Standard Classic v3 is now the default; see the [Classic contract](classic-output.md).
The CLI emits this C# extension only with `--format source`. The API uses `FrontendOutputFormat.Source`.
The extension format is `Morphir.CSharp.Source` version 1.
The envelope contains `format`, `formatVersion`, `classicModelVersion`, `distribution`, and `source`.
`classicModelVersion` is 3. It identifies the reused Classic algebra, not wire compatibility.
Source v1 is an explicit extension. A standard Classic v3 consumer cannot read its source-operation tags.
The separate Classic codec still emits unchanged standard v3 for libraries without source metadata.

`distribution` keeps package, dependency, module, type, access, and typed value containers.
Basic literals, variables, fields, tuples, conditional expressions, and direct arithmetic use Classic nodes.
An extended operation has this shape:

```text
["Source", resultType, kind, [[propertyName, propertyValue], ...], [orderedChildren, ...]]
```

Properties are strings. Child order is source order. Missing arguments are absent, not default values.
Each node describes written structure. It does not specify an execution algorithm.

## Source metadata

`source.declarations` contains selected outer type declarations in compilation/tree/source order.
Partial declarations remain separate entries. Nested types stay inside their enclosing declaration.
Each entry has `kind`, `token`, `symbol`, `type`, `location`, and `children`.
Tokens retain exact spelling, including escaped names, literal suffixes, modifiers, and tuple labels.
Nodes retain syntax kind and ordered children. No whitespace or comment round-trip is promised.
Locations contain file, start, length, line, and column. Positions refer to the acquired source input.
Symbols contain assembly identity and compiler documentation identity, or source position for local bindings.
Types contain the compiler-resolved source type, including nullable markers.
Source metadata is authoritative for distinctions absent from the Classic projection.
This includes class/struct/record form, source numeric width, parameter defaults, attributes, and declaration order.
Compiler-generated project syntax is input. Compiler-synthesized fields and equality bodies are not source declarations.

## Types

| Source | Projection |
| --- | --- |
| `bool` | SDK Bool |
| `char` | SDK Char and CharLiteral |
| `string` | SDK String |
| Integral domains, including native, 128-bit, BigInteger | SDK Int; exact source domain remains in metadata |
| Half, Single, Double | SDK Float; source precision remains in metadata |
| `decimal` | SDK Decimal and DecimalLiteral |
| Source class/struct/record | Source-owned named alias; source declarations retain full member structure |
| Source enum | Source-owned numeric alias; named members retain initializer expressions |
| Value tuple | Ordered Tuple; labels and family remain in metadata |
| Func, reference Tuple, Complex | Named source types in the intrinsic `Morphir.CSharp` type namespace |
| Params array | Intrinsic `Morphir.CSharp.Types.Array element` |
| Explicit `T?` or `Nullable<T>` | `Maybe T` |

The intrinsic type namespace names source types. It contains no executable compatibility library.
General arrays and the remaining framework families retain their delivery gates.
Declared types control parameters, fields, properties, and function results.
Flow state and possible runtime values do not change a declaration.
Inferred reference nullability does not insert Maybe on `var` or computed expressions.
Explicit source `?` is retained even with nullable analysis disabled.
An explicit `null` in a nonnullable slot is a typed Source Null operation. It does not widen the declaration.
This describes the source and makes no runtime validity claim.

## Operations

| Source form | Mapping |
| --- | --- |
| Integer/float arithmetic with compatible projected types | One SDK Basics operation |
| Decimal `+`, `-`, `*` | SDK Decimal add, sub, mul |
| Other arithmetic, shifts, bit operations, lifted or user operators | Source Binary with operator and ordered operands |
| Mixed projected numeric types | Source Binary; no implicit conversion steps |
| Unary operator | Source Unary |
| Explicit cast | Source Cast with target spelling |
| Parentheses, `!`, checked/unchecked | One source context node |
| Null/default/sizeof | Source Null, Default, SizeOf |
| Simple field/auto-property read | Field; metadata keeps original source access |
| Explicit accessor/nullable member read | Source MemberAccess; no body expansion or extraction |
| Named constants and external members | Source reference; no constant folding |
| Calls | Source Invocation, target MethodReference, and written Argument nodes |
| Method group | Source MethodReference; no lambda adapter or receiver snapshot |
| New and initializer | Source New and Initializer; no initialization execution |
| `?.`, `??`, `??=` | ConditionalAccess, Binary, and Assignment; no null-case expansion |
| Index | Source Index with written arguments |
| Assignment, deconstruction, with | Assignment, designation/tuple structure, and With |
| Blocks, locals, returns, if, empty statements | Source statement nodes in written order |
| Local function | Source LocalFunction within its lexical block |

Regular methods have declared inputs and output. An instance member has a minimum explicit receiver input.
Explicit constructors and accessor bodies remain separate definitions.
Parameter defaults and primary parameters remain in declaration metadata.
No constructor, getter, setter, factory, operator, or recursive body is executed during conversion.
No omitted optional argument or expanded params allocation is invented.

Names and module dictionaries use Morphir normalization. Source spelling and order remain in metadata.
Enum implicit initializers remain Source EnumMember nodes. No zero/increment expression is invented.
Wide literals outside signed 64-bit range use `SourceNumberLiteral` with their written token.
Special floating members remain named references. No invalid JSON numeric value is emitted.

## Admission and failure

Keep valid compiler binding, source ownership, namespace selection, collision checks, and the fixed toolchain.
Keep source project/reference graphs, generators, analyzer settings, and actual compiler options.
Check every selected definition, including private and unused definitions and written unreachable source.
Unsupported structural forms fail with source-located diagnostics. Output is all or nothing.
The [delivery index](../Morphir_CSharp_Frontend_delivery_plan.md) lists pending mappings.
Removing a proof requirement does not implement a missing syntax form.

Do not require null guards, numeric ranges, nonzero values, finite values, ownership, purity, or termination.
Keep explicit input mutation, reference identity, cycles, recursive calls, checked syntax, and scalar external calls.
Resolve external call symbols without translating DLL bodies.
Optional dependency mappings validate exact target identity and projected ABI. Pure/total flags do not gate admission.

The converter emits no numeric, floating, decimal, equality, dispatch, or heap simulation library.
Its output is for source analysis. CLR overflow, exceptions, rounding, allocation, and alias observations are not reproduced.

## Validation

`AuditMatrixTests` covers all 62 approved IDs. It compares complete selected token structure to the source.
`SourceStructureTests` asserts typed outputs, exact simple products, argument order, statements, and failure boundaries.
Integration tests cover all existing source fixtures, actual project/generator inputs, and atomic CLI output.
The independent [sample contract](../fixtures/source-structure/contract.json) states expected structures without converter-generated goldens.
Classic model/codec tests and frozen historical source/reference hashes remain active.
Former runtime execution tests and plans are [archived](history/runtime-contract/README.md), not current acceptance criteria.
