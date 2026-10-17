# Classic model and codec inventory — D1–D9 (D9 passed)

This inventory records delivered increments, not a complete-v3 claim. Classic and Json projects have no Roslyn dependency. D19 owns complete algebra/codec coverage; missing later nodes remain pending.

| Delivered model/codec | Reference / checks |
| --- | --- |
| Generic type/value attributes; Unit `{}`; typed aliases | `ModelTests`; every ordinary/codec distribution includes unit type attrs and typed value attrs |
| Name / Path / FQName | All parity cases; acronym/digit normalization; package-prefix/owner tests |
| Public / Private, documentation | `private`, `sample`, `model`; public specification filtering |
| Library, numeric formatVersion=3, package/dependency definitions/specifications | All complete distributions; `model` independent dependency codec case |
| Module/package definition/specification dictionaries | `model`, `private`, `sample`; arrays retain Elm key ordering |
| Reference / Function / Record type, record field objects | `eligibility`, `sample`; positional type-field order retained |
| TypeAliasDefinition / TypeAliasSpecification | `eligibility`, `private`, `model` |
| Typed value definitions/specifications, named ordered inputs | `eligibility`, `sample`, `model` |
| Bool / whole-number / string literals | `eligibility`, `sample` |
| Variable / Reference / Apply / Field / Record / IfThenElse | `eligibility`, `sample`, `model` |
| Strict complete parsed-JSON comparator | JsonTests: object order accepted; arrays, kind, access, docs, attrs, format, missing/null rejected; duplicate keys anywhere rejected; path/expected/actual reported |
| Deterministic UTF-8 atomic serialization | Atomic write test and CLI preservation/fresh-failure integration tests |

Explicit codecs are implemented directly against the pinned Morphir codec contract. Native dictionary/record serialization is not used for union cases or IR dictionaries. Unit has an explicit empty-object encoder; null has its own representation. Every unsupported model node lacks an implementation rather than being serialized through an implicit fallback.

D2 adds private-inclusive FQName definition lookup, exercised by independent `calls`, `project`, `internal`, and cumulative `sample` distributions. Existing Reference/Apply and public/private specification codecs cover the increment; no new union node is necessary. Source-reference compilation inputs remain outside IR. Complete v3 algebra coverage is still D19.

D3 adds generic typed `LetValue` encoded as the exact `LetDefinition` array with named value definition and body. D3 ordinary Elm Category and independent complete cumulative codec distributions verify the full nested definitions, output types, attributes and branch bindings. Immutable binding evaluation is checked separately against emitted C# behavior. No general recursive/local-function model is claimed.

D4 adds typed Constructor values, ConstructorPattern/AsPattern/WildcardPattern and PatternMatch, plus Maybe type references. Ordinary Elm and complete cumulative codec references verify all cases, attributes and ordered branches; Maybe absence is a constructor, never JSON null or an invented default. D19 full algebra coverage remains pending.

D5 adds finite FloatLiteral (nonfinite numeric JSON rejects), VariableType, ListValue, EmptyListPattern and HeadTailPattern. Full typed numeric helper libraries exercise these nodes in ordinary Elm output and independent complete codec distributions, including specifications, private imported values and alias definitions. Wide values use canonical exact limb records; NaN/infinities/negative zero are ordinary helper expressions rather than invalid JSON numeric literals. NumericTests also compares the generated production Classic factory with its complete pinned compiler output.

D6 adds exact string-payload DecimalLiteral and SDK Decimal type references. The model-only independent complete Elm codec fixture verifies DecimalLiteral even though business source constants lower to exact proved construction calls. Complete Decimal helpers and the source/cumulative distributions also verify every alias, value, typed attribute and access wrapper. See fixtures/D6 and DecimalCodecReferenceTests.

D7 adds Unit type/value/pattern and typed Lambda adapters for resolved method references and zero-argument Func thunks. Complete independent source/cumulative codec distributions verify these nodes, explicit receiver inputs, nested private local definitions and full external dependency specifications. General source lambdas/captures remain D13; full Classic algebra closure remains D19.

D8 adds Tuple types/values/patterns, TypeDefinition/TypeSpecification algebras, access-controlled CustomTypeDefinition constructor dictionaries, CustomTypeSpecification and OpaqueTypeSpecification. Separate alias definition/specification nodes preserve existing JSON exactly. Complete original Elm source/tuple/flags/cumulative/model references verify union tags, attributes, ordered dictionaries, source owners and hidden constructors. Production helper C# factories were re-emitted from unchanged original Elm IR solely for the typed container migration; existing golden JSON is unchanged. Full Classic algebra closure remains D19.

D9 increment adds the typed UpdateRecord node with the pinned explicit codec shape. Complete original Counter and six-form/private/recursive data distributions verify the full surrounding definitions, Maybe slots, attributes, record updates and recursive custom wrappers. Finite recursive record equality uses explicit pure definitions/references; no runtime heap or CLR identity helper enters IR. D9 passed its full delivery gates; complete algebra coverage remains D19.
