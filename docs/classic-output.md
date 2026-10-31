# Classic output

The CLI and core API return standard Morphir Classic v3 only.
Source v1 and its public CLI/API/model/codec paths are removed.
The [exact Elm parity contract](elm-parity.md) controls acceptance.

The JSON envelope contains only `formatVersion: 3` and `distribution`.
There is no source envelope, syntax tree, custom Source tag, or CLR compatibility library.
Internal C# staging nodes must be mapped or rejected before the frontend returns a library.

## Tested mappings

| C# input | Standard Morphir output |
| --- | --- |
| Supported record/class/struct data and stored fields/properties | Record aliases and Field reads |
| Positional record construction | Alias Constructor and ordered Apply nodes |
| Initialized locals and final returns | Nested LetDefinition and the return expression |
| Complete positional source calls and approved dependency calls | Reference and ordered Apply nodes |
| Integer/Float arithmetic, comparisons, Boolean operations, and negation | Direct SDK operations |
| Decimal add/subtract/multiply and negation | Direct Decimal SDK operations |
| Decimal literals | User-approved `Decimal.fromFloat` constructor calls |
| Explicit nullable type annotations | Maybe types; plain reference types remain plain |
| Value tuples and conditional expressions | Tuple and IfThenElse nodes |
| Parentheses, null-forgiving, checked/unchecked expression context | The enclosed mapped expression |

C# floating literals use the written numeric value, without Single runtime quantization.
The paired Elm Decimal inputs use explicit Float tokens, including `.0` for integral values.
This avoids the Elm whole-number parser's precision loss before `fromFloat` type inference.
No output JSON is rewritten to remove this difference.

The [case registry](../fixtures/elm-parity/cases.json) identifies the exact supported cases.
Each case compares the complete JSON to independently generated `morphir-elm make` output.
Integration tests run the pinned compiler again and compare its fresh output directly.
Names, package boundaries, visibility, types, literals, definitions, and array order are all compared.
Elm local annotations are written in the paired source input. Tests do not substitute type variables.

## Unmapped forms

Unmapped forms fail with a source-located diagnostic and no partial output.
An existing output file remains unchanged. There is no alternative output mode.

These forms retain explicit gaps: assignments/mutation, statement branches, object identity/cycles,
non-record primary constructors, explicit constructors/accessors/operators, overloaded method naming,
nullable value operations, general construction/initialization, enums, and later delivery forms.
Named/omitted/expanded arguments, captures/delegates, and conversions between different IR types also need approved mappings.
No runtime presence, range, ownership, effect, or termination proof is required for mapped operations.

D1–D9 have an exact-tested subset; their complete historical Source-mode scope is not accepted.
D10–D21 remain planned. See [current evidence](deliveries/elm-parity-state.md).
