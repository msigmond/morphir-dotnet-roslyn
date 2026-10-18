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

D10 is active. Ordinary source generic values reuse VariableType, parameterized AliasDefinition and typed reference/function attributes. Closed value/reference interfaces and inherited interface views reuse CustomDefinition constructors and MatchValue dispatch with explicit receiver inputs and M1 at reference boundaries. Scoped generic witnesses retain ordinary variables; type tests, guarded casts and nullable as use complete constructor/Maybe matches. Parameterized interface custom types retain argument substitutions; generic/default member functions forward required witnesses, with private default-body helpers under the original owner. Independent complete development references live under fixtures/D10; full D10 model/sample gates remain pending. See [D10 mapping](../mappings/D10.md).

D10 numeric conversion witnesses use ordinary FunctionType inputs and typed Lambda/Apply nodes; source/target variables stay parametric. The independent generic-factories distribution verifies those inputs and concrete adapters around the whole frozen numeric helper model. Floating-factories verifies exact saturation/rounding and IEEE/limb helper references. No new CLR-only codec metadata or runtime numeric dispatch enters IR. Full D10 acceptance remains pending.

The nineteenth independent D10 distribution verifies static abstract contract callback definitions and source implementation adapters with original data/member paths. Unit inputs model zero-argument callbacks; source static methods have no receiver input. Full static ownership/member and D10 cumulative gates remain pending.

The twentieth independent reference covers source static property getters and Unit callback adapters. Static reference results reuse existing source continuation/record/Maybe nodes, retaining live graph origins during lowering; no runtime heap schema or CLR-only metadata is emitted. Final D10 model/cumulative gates remain pending.

The active class/base increment reuses custom sums, typed match/function/reference nodes and private source body helpers. Runtime receivers remain explicit through base-body calls; no runtime heap or CLR identity metadata is added. The21st/22nd original distributions cover complete abstract override dispatch and direct base-body helpers. Full D10 model/sample acceptance remains pending.

The23rd complete D10 reference adds inherited field storage to the source leaf record and verifies explicit base projections, exact defaults and the whole dispatch/body model. It reuses existing record/field/custom/match nodes; no runtime heap schema or CLR metadata is introduced. The storage increment still awaits final parity/regression.


The24th complete original D10 distribution covers base initialization/construction using existing Record/UpdateRecord, CustomDefinition/Match and Function nodes. A protected abstract-owner constructor is a private explicit-receiver transformer over the complete leaf set; leaf allocation remains separate. No runtime heap/CLR metadata is introduced. D10 full model/sample gates remain pending.


The25th complete original D10 reference verifies reabstract/private dispatch, both closed interface domains and the otherwise-unused original default body using existing custom, match, reference and function nodes. Generic/static reabstraction reuses the current callback schema; full D10 model/sample gates remain pending.


The26th original complete D10 distribution verifies primary-base argument bindings, explicit protected base transformation, concrete allocation/update and inherited field extraction with existing Let/Record/UpdateRecord/Custom/Match nodes. No additional codec cases or runtime CLR metadata are needed; full D10 acceptance remains pending.


The27th complete distribution verifies distinct owner-qualified hidden storage labels and exact field types/accesses. The28th verifies both selected and unused constructor bodies, original private access, actual optional argument binding and full consuming code. Both reuse existing Name/Record/UpdateRecord/Let/Custom/Match nodes; no runtime heap schema or codec extension is introduced. Full D10 model/sample acceptance remains pending.

The29th independent complete D10 distribution covers nonvirtual inherited automatic property storage and original-owner construction transforms. Hidden properties use distinct source-owner field labels; custom getters/setters use ordinary functions and exhaustive class matches. Owned coalescing uses existing Maybe/conditional/record nodes. No new runtime heap schema is emitted; D10 final model and cumulative gates remain pending.

Independent references30 and31 cover complete instance getter and setter dispatch distributions from original pinned codecs. Getter functions return property values; setter functions return updated receiver payloads and reconstruct exhaustive contract constructors. Coalescing reuses existing Maybe matches, lets, functions and record updates. No heap schema or generated runtime metadata is introduced; boxed-value, nonsealed and final cumulative model gates remain pending.

The32nd independent complete concrete-base.json distribution represents a canonical concrete class as a closed custom type whose self alternative carries an anonymous record payload; derived leaf aliases keep their canonical source paths. Member functions remain in original owner modules. No source type is renamed to an invented storage type, and no field is dropped. Native and CLI increment checks passed; the complete cumulative gate remains pending.

Reference33, interface-boxes.json, independently covers an actual source box/update/getter distribution. Box identities are compile-time graph metadata; emitted IR remains ordinary constructors, records, updates, functions and lets. Shared alias updates and unboxing snapshots do not introduce a heap schema. Complete original parity, native and standalone CLI checks passed before the cumulative gate.
