# Classic output

The CLI and core API emit standard Morphir Classic v3 only.
The envelope has `formatVersion: 3` and `distribution`.
Source v1, custom source tags, and CLR helper libraries are not output formats.
The [exact Elm contract](elm-parity.md) controls acceptance.

| C# form | Approved Elm/Morphir form |
| --- | --- |
| Data fields and stored properties | Record aliases and Field reads |
| Positional record construction | Alias Constructor and Apply |
| Primary parameters; explicit all-field constructors | Same-named fields; record-building functions |
| Getter/operator bodies | Named functions and calls |
| Locals, returned branches, and conditionals | LetDefinition and IfThenElse |
| Local functions and immutable captures | Parameter lambdas; LetDefinition or LetRecursion |
| Method references, bound receivers, Func/Action | Function types, direct references, partial application, Apply |
| Named/optional/params calls | Parameter order, declared defaults, List packs |
| Explicit nullable declarations | Maybe; plain references stay plain |
| Null, nullable conversion, scalar default | Nothing, Just, corresponding constant |
| Coalesce, conditional access, lifted arithmetic | Maybe.withDefault, map, map2 |
| Presence/null tests | Comparison with Nothing |
| Integer/Float arithmetic and mapped casts | Direct SDK operations; no wrapping or range helpers |
| Decimal literals | Decimal.fromFloat |
| Nullable-result Decimal division | Decimal.div; no fallback |
| Enums | Named Int aliases and constant values |
| Value/reference tuples and written deconstruction | Tuple, Tuple.first/second, Destructure |
| Record equality and with-updates | Basics equality and UpdateRecord |

Declared types stay in definitions. Inferred locals use the written initializer type.
Maybe contexts can wrap a plain value in Just without changing its declaration.
No nullable flow, range, ownership, purity, or termination proof is required.
Zero-parameter Func/Action use the result type. No Unit argument or lambda adapter is inserted.
Floating literals use their written value without CLR Single quantization.
Paired Elm Decimal inputs use Float tokens, including `.0` for integral values.

Every admitted feature has paired C#/Elm input and complete compiler JSON equality.
The [registry](../fixtures/elm-parity/cases.json) retains names, access flags, types, definitions, literals, and array order.
Only whitespace and object-key order are ignored. Tests recompile the Elm projects with the pinned compiler.

Unmapped forms fail at the source and leave existing output unchanged.
The [rejection inputs](../fixtures/elm-parity/form-rejections.json) cover mutation, identity/cycles,
recursive aliases, incomplete/default construction, general initializers, Nullable.Value without a fallback,
nullable flattening/fallbacks, unlisted numeric forms, and larger tuple projections.
The [member cases](../fixtures/elm-parity/member-rejections.json) retain all unapproved D5/D6 signatures.
Two native unsigned operator forms fail C#14 binding. The frontend retains those compiler errors.
Elm name shadowing and normalized identity collisions remain errors.
Later generic, collection, pattern, anonymous-function, loop/ref, exception, and framework forms keep their delivery gates.

D1–D9 implement the [approved profile](reviews/d1-d9-elm-mapping-decisions.md).
D10–D21 remain planned. See [validation](deliveries/elm-parity-state.md).
