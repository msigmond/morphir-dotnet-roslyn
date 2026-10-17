# Classic model and codec inventory — D1–D3

This is the D1 increment, not a complete-v3 claim. Classic and Json projects have no Roslyn dependency. D19 owns complete algebra/codec coverage; missing later nodes remain pending.

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
