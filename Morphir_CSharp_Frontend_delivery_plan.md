# Morphir C# frontend — master execution plan

- status: D1–D2 passed; D3–D21 planned; implementation target: this repository
- updated: 2026-10-16
- executor target: GPT-6.1 Sol / High
- authority: this master → active delivery → implementation evidence
- source conversion: former delivery plan + examples; original paired code retained in children
- required scope decisions: closed; routine implementation choices recorded with evidence

## Execution protocol

- context minimum: master + active child + prerequisite evidence + affected code/catalogs
- child selection: explicit requested delivery, otherwise first unpassed delivery in table order
- execution unit: active child; future deliveries pending; prerequisite fixes only as required by active gates
- entry gate: prerequisite delivery evidence; no planned prerequisite treated as implemented
- workflow: inspect → mapping/proof contract → minimal complete vertical feature → independent fixtures → checks → catalog/state update
- delivery boundaries: additive; prior support retained; model increments only as needed through D18.4; completeness in D19
- internal task subdivision: permitted; whole delivery pending until all acceptance gates
- child implicit gates: master delivery gates + prerequisite completion + local acceptance/rejections
- child completion update: checklists/status + delivery evidence; no copied boilerplate handoff sections
- examples: semantic seeds; complete emitted structure/helpers/reference fixtures still required
- compatibility changes: explicit recorded review; no silent baseline/scope/reference upgrade
- failed/unfinished gate: pending work + source diagnostic/evidence; no completion claim
- handoff state: `docs/deliveries/<ID>-state.md`; status `planned|active|passed`; implementation paths, commands/results, fixture provenance, unresolved required work
- independent validation: expected JSON unchanged during validation; regeneration separate, explicit, reviewed

## Delivery index

| Delivery | Capability | Prerequisites |
| --- | --- | --- |
| [D1](Morphir_CSharp_Frontend_deliveries/D01-cli-record.md) | CLI, record data, first typed distribution | — |
| [D2](Morphir_CSharp_Frontend_deliveries/D02-project-inputs.md) | Project compilation inputs and coverage catalogs | D1 |
| [D3](Morphir_CSharp_Frontend_deliveries/D03-blocks-locals.md) | Blocks, locals, decisions | D2 |
| [D4](Morphir_CSharp_Frontend_deliveries/D04-maybe.md) | Missing values and M1 presence proofs | D3 |
| [D5](Morphir_CSharp_Frontend_deliveries/D05-numerics.md) | Numeric domains, conversions, unchecked arithmetic | D3 |
| [D6](Morphir_CSharp_Frontend_deliveries/D06-decimal.md) | Exact Decimal construction and safety | D4, D5 |
| [D7](Morphir_CSharp_Frontend_deliveries/D07-calls.md) | Resolved calls, overloads, parameters | D2, D3, D4, D5, D6 |
| [D8](Morphir_CSharp_Frontend_deliveries/D08-enums-tuples.md) | Enums and tuples | D5, D7 |
| [D9](Morphir_CSharp_Frontend_deliveries/D09-user-data.md) | Classes, structs, records, nested data | D7, D8 |
| [D10](Morphir_CSharp_Frontend_deliveries/D10-generics-dispatch.md) | Generics, interfaces, inheritance | D9 |
| [D11](Morphir_CSharp_Frontend_deliveries/D11-collections.md) | Arrays and collection data | D4, D10 |
| [D12](Morphir_CSharp_Frontend_deliveries/D12-patterns.md) | Complete pattern matching | D8, D9, D10, D11 |
| [D13](Morphir_CSharp_Frontend_deliveries/D13-closures.md) | Lambdas, delegates, captures | D7, D10 |
| [D14](Morphir_CSharp_Frontend_deliveries/D14-recursion.md) | Self and mutual recursion | D7, D13 |
| [D15](Morphir_CSharp_Frontend_deliveries/D15-loops-local-state.md) | Loops, local mutation, ref/out/in | D11, D14 |
| [D16](Morphir_CSharp_Frontend_deliveries/D16-framework-data.md) | Dates, times, durations, identifiers | D6, D11 |
| [D17](Morphir_CSharp_Frontend_deliveries/D17-type-closure.md) | Object boxing and type-catalog closure | D8, D9, D10, D11, D12, D13, D14, D15, D16 |
| [D18.1](Morphir_CSharp_Frontend_deliveries/D18_1-caught-failures.md) | Caught failures and exception flow | D3, D4, D5, D6, D7, D8, D9, D10, D11, D12, D13, D14, D15, D16, D17 |
| [D18.2](Morphir_CSharp_Frontend_deliveries/D18_2-lazy-iterators.md) | Pure lazy iterators | D18.1, D11, D12, D13, D14, D15 |
| [D18.3](Morphir_CSharp_Frontend_deliveries/D18_3-dynamic.md) | Statically resolved pure dynamic operations | D18.2, D7, D10, D17 |
| [D18.4](Morphir_CSharp_Frontend_deliveries/D18_4-feature-closure.md) | Admitted feature closure and exclusions | D18.1, D18.2, D18.3 |
| [D19](Morphir_CSharp_Frontend_deliveries/D19-classic-complete.md) | Complete v3 Classic model and serializer | D18.4 |
| [D20](Morphir_CSharp_Frontend_deliveries/D20-msbuild.md) | MSBuild adapter | D19, D2 |
| [D21](Morphir_CSharp_Frontend_deliveries/D21-acceptance.md) | Complete frontend acceptance | D20 |

- current execution: D1–D2 passed; next delivery D3; [D2 evidence](docs/deliveries/D2-state.md); [D1 implementation/gate evidence](docs/deliveries/D1-state.md)
- D18 grouping: D18.1 → D18.2 → D18.3 → D18.4; no separate D18 implementation delivery
- test trait: `Delivery=<ID>`; example `Delivery=D18.1`
- validation minimum: active-delivery tests + affected prerequisite regression suites; D21 full clean suite

## Project layout

```text
Morphir.CSharp.slnx
LICENSE                             # mirrored finos/morphir-dotnet license
NOTICE                              # preserved upstream attribution
README.md                           # current supported behavior + commands
AGENTS.md                           # canonical repository instructions
CLAUDE.md                           # @AGENTS.md only
global.json                         # exact .NET SDK
Directory.Build.props               # C# 14, reproducibility, common build settings
Directory.Packages.props            # exact managed dependency pins
src/
  Morphir.IR.Classic/               # Roslyn-free algebra; generic attributes/typed aliases
  Morphir.IR.Json/                  # Roslyn-free explicit format-v3 codecs
  Morphir.Frontend.CSharp/           # Compilation + settings -> typed IR + diagnostics
  Morphir.Frontend.CSharp.ProjectSystem/ # D2: shared project inputs/generators
  Morphir.Frontend.CSharp.Cli/       # D1: source CLI; D2: project adapter
  Morphir.Frontend.CSharp.MSBuild/   # D20: task, package, targets
tests/
  Morphir.IR.Classic.Tests/
  Morphir.IR.Json.Tests/
  Morphir.Frontend.CSharp.Tests/
  Morphir.Frontend.CSharp.IntegrationTests/
fixtures/
  <ID>/
    csharp/                         # accepted source/project inputs
    rejected/                       # isolated expected failures
    elm/                            # independent ordinary Elm source
    codec-reference/                # independent complete Elm IR/codec host cases
    expected/                       # complete v3 JSON + provenance
samples/
  CurrentSupportedExample/
    CurrentSupportedExample.csproj
    CurrentSupportedExample.cs      # cumulative supported-language class + data
eng/                                # pinned reference generation + parity/build tooling
docs/
  catalogs/                         # language, framework members, model/codec coverage
  mappings/                         # per-feature semantic/proof contracts
  deliveries/                       # evidence + resumable state per delivery
```

- dependencies: Json → Classic; core → Classic + Roslyn; ProjectSystem → Roslyn/project tooling; adapters → core + Json + shared inputs
- acquisition/serialization: adapters/shared project inputs; conversion rules: core only
- syntax: declaration discovery/source locations; symbols: identity/types/calls; IOperation + CFG: semantics
- output: typed model + diagnostics; unsupported selected source → failure; no silent partial distribution
- prototype: [archived addition regression](docs/references/prototype-addition/PROVENANCE.md); no runtime dependency
- bootstrap foundation retained; D1–D2 implementation passed ([D1 evidence](docs/deliveries/D1-state.md), [D2 evidence](docs/deliveries/D2-state.md)); D3–D21 pending
- CLI first; MSBuild D20; further environments/deserialization outside current scope

## Repository artifacts — D1 onward

- cumulative example: `samples/CurrentSupportedExample/CurrentSupportedExample.cs`; public static class `CurrentSupportedExample`; package `Example`; selected namespace `Example.CurrentSupported`
- reference pattern: [Scala CurrentSupportedExample](https://github.com/msigmond/morphir-jvm/blob/3086f7ad501d13239b474e23baa3d7fc88baf881/morphir/frontend/examples/src/CurrentSupportedExample.scala); feature coverage pattern only; existing C# scope/exclusions unchanged
- example coverage: every currently transpiled language feature; named methods/sections; necessary companion data declarations; source owner paths retained
- incremental maintenance: new supported forms each delivery; previous forms retained; delivered forms only; no commented-out/future code as coverage
- coverage map: `docs/catalogs/language.md`; supported form → example symbol/location + introducing delivery + parity/behavior fixtures; zero unmapped supported forms
- sample verification: actual C# compilation + whole selected-namespace transpilation + complete-distribution parity + applicable behavior checks; host runner/tests outside selected business namespace
- model/adapter-only delivery: phase marker + coverage/evidence refresh; full cumulative verification; explicit no-language-delta record; no invented business-source forms
- license provenance: [bootstrap source/hash record](docs/references/LICENSING.md)
- license: root `LICENSE` mirrored from [finos/morphir-dotnet](https://github.com/finos/morphir-dotnet); upstream `NOTICE` attribution retained; source URL/commit + copied-file hashes in D1 evidence; license text/copyright preserved
- README: purpose; delivery/support status; pinned prerequisites; project layout; build/test/reference-generation commands; CLI usage; cumulative sample; selection/safety/exclusions; MSBuild usage when delivered; license + plan/catalog links
- README maintenance: each delivery; executable verified commands; delivered/pending distinction; no stale capability claims
- AGENTS: canonical concise repository instructions; layout; master/active-child workflow; actual build/test/parity commands; semantic/safety/naming/reference rules; sample/catalog/doc maintenance; completion evidence
- AGENTS maintenance: each delivery; commands/layout/gates synchronized with implementation
- CLAUDE: exact body `@AGENTS.md` + newline; pointer only; no copied rules
- ownership: D1 bootstrap; every delivery maintenance; D21 complete coverage/repository-artifact audit

## Fixed baseline and scope

- frontend: .NET 10; C# 14; explicit 64-bit native target independent of host; incompatible x86 target rejected
- exact SDK/Roslyn/packages: pinned at D1; no automatic version-driven scope expansion
- prior example compilation evidence: SDK 10.0.112; implementation pins/evidence still required
- reference: Morphir Elm `2.100.0`; source `71ca7ee385273b0af6f8db66582c26e9b410cbf2`; Classic versioned distribution `formatVersion: 3`
- source scope: all user-defined business data + language types + explicit framework catalog + pure functions under contracts below
- language primitives: bool/char/string/object/dynamic; byte/sbyte/short/ushort/int/uint/long/ulong/nint/nuint/float/double/decimal
- language data: arrays; nullable; value/reference tuples; delegates; type parameters; classes/structs/record classes/record structs/enums/interfaces; applicable generics/inheritance
- framework type/version coverage: catalog below; no automatic XML/JSON trees, tables, reflection metadata, runtime handles, memory views or other families
- member purity/semantics distinct from type representation; read-only interface ≠ proof of immutable backing data
- final support: pure members of admitted data under agreed domain/safety rules; arbitrary unsupported admitted forms block release

| Family | Accepted entries | Delivery |
| --- | --- | --- |
| Additional numbers | `System.Half`, `System.Int128`, `System.UInt128`, `System.Numerics.BigInteger`, `System.Numerics.Complex` | D5 |
| Dates and durations | `System.DateOnly`, `System.TimeOnly`, `System.DateTime`, `System.DateTimeOffset`, `System.TimeSpan` | D16 |
| Identifiers | `System.Guid` | D16 |
| Generic collections | `System.Collections.Generic.List<T>`, `Dictionary<TKey,TValue>`, `HashSet<T>`, `Queue<T>`, `Stack<T>` | D11 |
| Collection interfaces | `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, `IDictionary<TKey,TValue>`, `ISet<T>` in `System.Collections.Generic` | D11 |
| Read-only interfaces | `IReadOnlyCollection<T>`, `IReadOnlyList<T>`, `IReadOnlyDictionary<TKey,TValue>`, `IReadOnlySet<T>` in `System.Collections.Generic` | D11 |
| Read-only wrappers | `System.Collections.ObjectModel.ReadOnlyCollection<T>`, `ReadOnlyDictionary<TKey,TValue>` | D11 |
| Immutable collections | `ImmutableArray<T>`, `ImmutableList<T>`, `ImmutableDictionary<TKey,TValue>`, `ImmutableHashSet<T>`, `ImmutableQueue<T>`, `ImmutableStack<T>` in `System.Collections.Immutable` | D11 |

- per catalog entry: exact framework/package version; representation; pure-member inventory; semantic boundaries; owning delivery; positive/negative/boundary fixtures
- catalog additions: explicit scope change; no hidden unsupported members under a complete-support label

## Selection and identity

- selectors: complete case-sensitive namespace segments; selected namespace + all descendants; overlapping selectors deduplicated
- example: Sales → Sales + Sales.Tax; SalesTax excluded; Sales.Total → Shared.Round requires explicit Shared selector
- selected set: every admitted source data type/function, unused + nonpublic included; attributes/public reachability not inclusion gates
- source origins: handwritten, configured generators, source project references; project reference alone not namespace selection
- outside source dependency: diagnostic with required symbol, namespace, requester location; failure until selected; then all admitted declarations/descendants included
- external calls: exact catalog mapping or configured Morphir dependency specification; no DLL-body translation/purity inference
- compiler plumbing: represented by source lowering; no standalone backing-field/state-machine implementation exports
- package: required explicit setting; upstream Name/Path normalization; no inferred directory/machine identity
- data definition owner: namespace module; nested type owner: enclosing-type module
- member owner: class/type module; static containers retained; instance receiver explicit input
- enum constants: enum member module; enum data definition unchanged
- partial/generated declarations: same source type combined; unrelated containers never merged
- relative module path: configured package prefix removed only on matching source owner prefix
- normalization/collisions: deterministic type/member/generic-arity disambiguation; callable suffix from full canonical signature including generic arity + parameter/ref kinds; remaining collisions → both source locations
- resolution: shared symbol→definition table; data/member identities separate; declaration/import/call/type-reference paths checked

| Source symbol | Definition module | Local definition | Full Elm symbol |
| --- | --- | --- | --- |
| Example.D4.Customer | Example.D4 | Customer type | Example.D4.Customer |
| Example.D10.MinimumAge | Example.D10 | MinimumAge type | Example.D10.MinimumAge |
| Example.D10.MinimumAge.Allows | Example.D10.MinimumAge | allows function | Example.D10.MinimumAge.allows |
| Example.D4.Rules.Adult | Example.D4.Rules | adult function | Example.D4.Rules.adult |
| Example.D8.Access.Read | Example.D8.Access | read value | Example.D8.Access.read |

- forbidden identity changes: Customer.Customer/MinimumAge.MinimumAge; member flattening; function-name prefixes replacing containers
- generated Example.Compat types/helpers: explicit framework/state mapping; no relocated source data
- illustrative overload/helper spelling: final canonical mappings + complete helper bodies in reference fixtures

## Access and project inputs

- v3 access: Public / Private only; public C# effective accessibility including enclosing types → Public; all other source accessibility → Private where wrappers available
- complete definitions: selected public + private; public specifications: public surface only; no invented Internal tag/extra CLR JSON metadata
- source accessibility: Roslyn; nonpublic cross-container references retained with Private metadata + private-inclusive lookup
- forbidden visibility workarounds: merged modules, promoted private declarations, stripped metadata, patched generated JSON
- ordinary Elm limitation: unexposed function inaccessible across modules; independent complete Elm codec fixtures for legal C# combinations
- compilation inputs: selected framework, actual options/symbols/references/nullable/overflow settings/additional files/generator inputs
- generated source: captured build output or equivalent explicit generator execution; workspace loading alone insufficient
- compiler/generator errors: fail conversion
- MSBuild task: no assumed live compiler Compilation; equivalent task-owned compilation/generator pass absent supported handoff

## Value semantics, ownership, purity

- classes: value data; constructors/field/property reads/pure methods/local updates supported; input receiver = input data
- mutable declarations: admitted; allowed operations/ownership decisive
- identity: ReferenceEquals/reference-identity equality rejected; approved value equality preserved
- recursive types + finite acyclic data: admitted; cyclic object graphs rejected
- local reassignment/counters/loops/new owned object/collection mutation: admitted
- local aliases: shared update observations preserved via functional state/bindings; no exposed CLR identity
- input/global/captured external reachability: immutable from business function; new wrapper ≠ new ownership of input reference
- reads/effects rejected: mutable external state; clock/randomness; I/O/resources; other observable effects
- purity/ownership/aliases/effects: interprocedural through constructors/getters/operators/helpers/calls; unknown behavior → diagnostic
- immutable captures: admitted; externally visible persistent mutable closure state rejected; wholly local effects need ownership proof

## Safety policies

| Contract | Required behavior |
| --- | --- |
| checked | actual syntax rejection: expressions, blocks, checked operator declarations; even constant-safe/unreachable; comments/strings/@checked unaffected |
| checked semantic operations | explicit/compilation-enabled checked arithmetic/conversions rejected; settings retained; no unchecked reinterpretation/helper generation |
| Maybe M1 | null → Maybe; tests/coalescing/conditional access/defaults/lifted behavior; present-value proof on every extraction/dereference |
| nullable representation | possibly null fields/elements/default slots → Maybe despite annotations; absence removed only by proven construction/domain/flow invariant |
| M1 evidence | dominating guards/HasValue; invalidation via assignments/aliases/calls; !/annotation alone no proof |
| M1 rejection | unproved access; no invented default, Nothing substitution, Result wrapper or hidden exception fallback |
| Decimal D3 policy | exact construction incl. 0.1m; C# successful precision/rounding/scale/conversion/range semantics |
| Decimal safety | proof for overflow/division/conversion/member failures; constants/validated bounds/guards; nonzero divisor alone insufficient |
| Decimal helpers | SDK only after semantic verification; valid-IR compatibility helpers otherwise; fromString Maybe extraction under M1 |
| Decimal rejection | unproved safety, undocumented bounds, saturation/default/error wrapper/differing arithmetic adoption; intrinsic Decimal overflow governed by D3 |
| other source failures | integer division/remainder, bounds, missing keys, parsing, casts, nonexhaustive match: proved safe or all failures explicitly handled |

- policy priority: checked syntax always rejected; M1 + Decimal safety mandatory even inside catch; no blanket checked-flag rejection of all intrinsic Decimal arithmetic
- failed proof: stable source-located diagnostic + whole conversion failure; no new success artifact
- pre-D18.1: earlier failure-prone features restricted to proven-safe subset
- admitted domain: explicit contract/evidence; no unstated business bounds

## Advanced features and exclusions

- exceptions: pure throw/try/catch/filter/rethrow/finally; handler selection/order; local state; finally on normal/early-return/failure paths
- failure representation: explicit private failure/control-flow data/helpers; ordinary public business return types retained
- handling: private helper failure to proven handler permitted; selected public function with unhandled failure rejected even when a caller catches it
- exception scope: no arbitrary framework exception APIs or identity operations added
- iterators: pure yield return/break; lazy steps; argument/capture/local state; ordering; fresh/repeated/nested enumeration; early disposal/finally; analysis at execution/consumption points
- iterator substitution: eager lists forbidden for deferred semantics
- dynamic: known closed runtime target set; exact member/operator/conversion binding, purity, evaluation order; unknown/unproved binding failure rejected
- exclusions: LINQ calls/query syntax; checked overflow; async/await/async iteration; pointers/stackalloc/ref-like declarations/uses incl. spans/ref structs; identity; cycles; input/external mutation/effects
- ordinary ref/out/in: admitted with local ownership; distinct from ref-like data exclusion
- other source features: outside confirmed pure/data contracts excluded; no implication of universal C# from complete IR model

## IR, JSON and reference policy

- model: C# Classic algebra; every node reachable from v3 distribution; modern C# implementation syntax permitted; generic type/value attributes + typed aliases
- serializer: explicit codecs; no dependency on unfinished morphir-dotnet serializer
- envelope: numeric formatVersion=3; Library distribution; package path; dependency specifications; package definition
- identity encoding: Name token arrays; Path arrays of Name; FQName upstream structure
- union encoding: tagged arrays; exact case/position; dictionaries: upstream representation + key order, not default dictionary serialization
- attributes: type attributes unit {}; value attributes types; Decimal literal payload string
- JSON: UTF-8, deterministic, atomic successful write; every field preserved
- equality: complete parsed distributions; whitespace/object-property order ignored only
- objects: exact keys + values; duplicate keys invalid; missing ≠ null
- arrays: exact length/order; dictionary-entry arrays included; no sorting
- scalars: exact JSON kind/value; case-sensitive strings; null ≠ {} ≠ []
- compared metadata: names/package/modules/docs/access/dependencies/typed attributes/helpers; no stripped/normalized differences
- mismatch: JSON path + expected/actual; parity failure
- ordinary references: independent paired Elm source; pinned compiler/mode/provenance
- default compiler: ordinary/private-helper fixtures
- D14 all-public mutual recursion: fallback; default ValueCycleDetected limitation
- fallback limitation: unexposed functions emitted Public; no fallback private-helper reference or JSON correction
- independent codec references: required source-inexpressible nodes/access combinations/combined fixtures affected by documented compiler limits
- codec construction: complete distribution; actual business bodies/qualified references/access wrappers/dependencies; no patched compiled JSON, private promotion or omitted fields
- model-only fixtures: host code; no new business-source API permission

## Feature record and delivery gates

- mapping record: C# forms; exact bound symbols/overloads; admitted domain; success/failure behavior; IR representation; proof rules; full helper bodies/dependencies; paired Elm; owner/status
- unchecked numeric record: widths/wrap/promotions/conversions/transport; checked = rejection only
- Maybe record: nullable nesting/defaults/presence proofs; Decimal record: exact construction/success semantics/safety proofs
- framework call record: exact overload + explicit map or translated available pure implementation; matching name insufficient
- model/codec inventory: maintained each delivery; full required inventory in D19

- [ ] Active feature + full assigned catalog scope working; no seed-example-only completion
- [ ] Independent source/codec references under above policy; full distribution structural equality
- [ ] Comparator regressions: reordered object fields accepted; reordered arrays/missing-null/scalar-kind changes rejected
- [ ] Source dependence: changed names/operands/branches/fields reflected in IR
- [ ] Declaration/import/call/type-reference owners correct; partial/private/enum behavior retained
- [ ] Generated helpers + specifications + access/docs/attributes/dependencies compared
- [ ] Meaningful affected rejection cases; unsupported/impure selected forms fail clearly
- [ ] Local alias updates positive; input/external mutation negative incl. helper/new-wrapper reachability
- [ ] M1 + Decimal safety positive/negative; no hidden wrappers/defaults
- [ ] Failure/recovery proofs; lazy timing/dynamic resolution where delivered; catch no safety bypass
- [ ] C# behavioral boundaries; JSON equality alone insufficient
- [ ] Exact version/mode/fixture provenance; no automatic reference refresh
- [ ] Support/mapping/model catalogs; runnable sample; reproduction commands; delivery state evidence
- [ ] CurrentSupportedExample updated through active delivery; every supported language form mapped; cumulative compile/transpile/parity/behavior evidence
- [ ] README/AGENTS current; verified commands; CLAUDE pointer-only; mirrored license/attribution intact

## Final gates — D21

- [ ] Every admitted fixed-version type/member/pure form through CLI + MSBuild; zero pending required entries
- [ ] .NET 10/C#14/exact pins reproducible; 64-bit native semantics; incompatible x86 settings rejected
- [ ] Every selected unused/nonpublic declaration emitted; correct access/owner paths; no synthetic repeated type names
- [ ] Descendant/segment selectors; explicit outside-source boundary; no implicit reference/container inclusion
- [ ] Class value semantics; local mutation/aliases; identity/cycle/external mutation rejection
- [ ] M1 safety; exact Decimal + D3 proofs; no error wrappers
- [ ] Exception/filter/rethrow/finally, lazy iterator/disposal, closed pure dynamic: parity + behavior
- [ ] Possible public unhandled failure rejected; all agreed exclusions diagnosed through both adapters
- [ ] Complete v3 algebra/serializer coverage; source + independent complete codec distribution parity
- [ ] No silent selected logic loss; evidence at semantic boundaries
- [ ] Clean checkout checks + packages/tools/support docs/upgrade procedure; no unresolved required scope decisions
- [ ] Cumulative example covers every transpiled language feature; README/AGENTS complete; CLAUDE pointer-only; mirrored license/attribution included in release artifacts as applicable

## Existing evidence — limitations

- prototype: one-file Roslyn symbols/IOperation; narrow two-int addition; no general overflow proof; JsonNode builders; no Classic model/project adapter
- legacy compiler: 2.96.1; addition-only 2.100.0 comparison matched; no broader compatibility claim
- historical prototype verification: sample + 72 passing tests; no complete frontend claim
- paired examples, 2026-10-14: 27 accepted C# blocks compiled with SDK 10.0.112; rejected block separate
- namespace audit: 11 source data types, 25 static containers, 48 methods, 4 enum constants; declaration/import owners checked
- Elm: 42 blocks syntax; 41 business modules v3 generation (40 default + D14 fallback); D19 host syntax/owner separately
- generated IR audit: source type/member/enum paths; D10 receivers; private helpers
- frontend C#/Elm complete parity: planning examples alone were unverified; D1 now has eight independent complete distribution references and passed parity/behavior evidence; later deliveries remain unverified
- D11 Array clarification: child-local verified toolchain limitation + dependency distinction; compatibility sample retained

## Contract sources

- [Published package](https://www.npmjs.com/package/morphir-elm).
- [Versioned distribution and current format version](https://github.com/finos/morphir-elm/blob/71ca7ee385273b0af6f8db66582c26e9b410cbf2/src/Morphir/IR/FormatVersion.elm).
- [Versioned distribution codec](https://github.com/finos/morphir-elm/blob/71ca7ee385273b0af6f8db66582c26e9b410cbf2/src/Morphir/IR/FormatVersion/Codec.elm).
- [Distribution codec](https://github.com/finos/morphir-elm/blob/71ca7ee385273b0af6f8db66582c26e9b410cbf2/src/Morphir/IR/Distribution/Codec.elm).
- [Type algebra](https://github.com/finos/morphir-elm/blob/71ca7ee385273b0af6f8db66582c26e9b410cbf2/src/Morphir/IR/Type.elm).
- [Value and pattern algebras](https://github.com/finos/morphir-elm/blob/71ca7ee385273b0af6f8db66582c26e9b410cbf2/src/Morphir/IR/Value.elm).
- [Literal codec](https://github.com/finos/morphir-elm/blob/71ca7ee385273b0af6f8db66582c26e9b410cbf2/src/Morphir/IR/Literal/Codec.elm).
- [Unit codec](https://github.com/finos/morphir-elm/blob/71ca7ee385273b0af6f8db66582c26e9b410cbf2/src/Morphir/Codec.elm).
- [Decimal SDK construction and operations](https://github.com/finos/morphir-elm/blob/71ca7ee385273b0af6f8db66582c26e9b410cbf2/src/Morphir/SDK/Decimal.elm).
- [Roslyn semantic analysis](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/work-with-semantics).
- [MSBuild tasks](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-tasks).
- [Access algebra](https://github.com/finos/morphir-elm/blob/71ca7ee385273b0af6f8db66582c26e9b410cbf2/src/Morphir/IR/AccessControlled.elm); [access codec](https://github.com/finos/morphir-elm/blob/71ca7ee385273b0af6f8db66582c26e9b410cbf2/src/Morphir/IR/AccessControlled/Codec.elm)
- [Package definition/specification](https://github.com/finos/morphir-elm/blob/71ca7ee385273b0af6f8db66582c26e9b410cbf2/src/Morphir/IR/Package.elm); [Elm modules](https://guide.elm-lang.org/webapps/modules)
- [.NET/C# language versions](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-versioning)
- [TASTy container reference](https://github.com/msigmond/morphir-jvm/blob/3086f7ad501d13239b474e23baa3d7fc88baf881/morphir/frontend/src/morphir/codegen/tasty/TypeDefMorph.scala): static grouping retained; repeated data module/type placement excluded
