> Historical runtime evidence. Current acceptance: [exact Elm parity](elm-parity-state.md).

# D9 — execution evidence

- Status: passed. D1–D8 prerequisites passed and D8 publication f604f6a1ce840e63781b3975ab980b6b6cac6ea8 was verified with authenticated gh.
- Baseline unchanged: SDK10.0.100, C#14, .NET reference pack10.0.0, Roslyn5.0.0, x64 CLI. No version/scope upgrade, golden adoption or license-byte changes.
- Implementation: all six class/struct/record/readonly data forms; source/nested/private owners; fields/auto/field-backed/custom properties, getters/indexers/setters/init; explicit/primary/default/copy constructors, initialization/chaining; record/struct with; local alias/value-copy and mixed-location updates; finite recursive layouts; resolved pure operators/conversions, lifted/conditional forms and exact CLR value equality. Const fields retain named source definitions outside instance storage.
- Ownership: branch environments hold immutable cell versions. Class aliases share updates; struct copies retain child reference reachability. Source factory/getter/helper/operator results retain caller origins. Input/global/external reachability is immutable, including through fresh wrappers. Unknown external result aliases establish no independence. Cyclic graphs and identity observations reject; no runtime heap/identity helper, invented default or error wrapper enters IR.
- Access/semantics: complete Public/Private definitions, private-inclusive references and public specifications; constructor/getter/operator/initializer purity checked even when unused; M1 extraction requires proof. Captured primary parameters are actual receiver state. Copy construction preserves its initialization policy and shared children. Equality preserves NaN/Decimal/tuple/Complex and boxed-type boundaries; custom source bodies remain resolved implementations.
- Later prerequisites remain explicit: generics/interfaces/open dispatch D10; collections D11; general patterns D12; mutable captures D13; source recursion D14; object/general boxing D17; remaining language/member closure owners. Reference-record generated operators whose custom Equals might observe their identity shortcut reject MCS3003; direct custom Equals and explicit value operators are preserved.

## Independent references

Four complete original Elm codec distributions are frozen under fixtures/D9/expected: counter.json, data-forms.json, constants.json, sample.json. Original hosts, source hashes, pinned Morphir2.100.0/commit71ca7ee385273b0af6f8db66582c26e9b410cbf2 and exact modes are in provenance.json. Generation was a separate reviewed operation before validation; no frontend output was used. All D1–D8 reference bytes remain unchanged. D9 generation refuses changed existing output/source bytes.

The cumulative Data.cs adds six forms, private/recursive owners, source constructors/getters/operators/conversions, record equality/with and owned alias updates while preserving all prior sample features. reference.json points to the complete D9 sample reference. Actual project/source-reference/source-generator and CLI modes match the whole frozen distribution.

## Final gates

- Locked restore: dotnet restore Morphir.CSharp.slnx --packages .packages --locked-mode — passed; artifacts/d9-final-restore.log.
- Build: dotnet build Morphir.CSharp.slnx --no-restore -m:1 — passed, zero warnings/errors; artifacts/d9-final-build.log.
- Full D1–D9 suite: dotnet test Morphir.CSharp.slnx --no-build --no-restore -m:1 --filter 'Delivery=D1|Delivery=D2|Delivery=D3|Delivery=D4|Delivery=D5|Delivery=D6|Delivery=D7|Delivery=D8|Delivery=D9' — passed200, zero failures/skips (150 integration,27 frontend,4 Classic,19 Json); artifacts/d9-final-tests.log and delivery-D9-final TRX reports.
- Focused D9 suite —39 passed; artifacts/d9-direct-copy-tests.log. Native boundaries cover aliasing/copies, constructor order/early return/copy bodies/primary captures, setter/indexer order, nested tuples, nullable/reference guards, recursive finite data, IEEE/Decimal equality and input/identity/effect/cycle rejection.
- Source CLI — complete Counter/data-form/constant parity; artifacts/d9-final-cli-fac975eb63804288a9f21fdbc0fbfb4c. Four rejection families preserve existing bytes and produce no fresh artifact; artifacts/d9-cli-domain-892e1cbb4a524e49adf71252c61ceb03.
- python3 eng/check-repository.py, python3 eng/check-catalogs.py, git diff --check and staged whitespace — passed. All24 plans/paired code, links/fences, reference hashes, protected license/NOTICE and CLAUDE bytes remain valid. Final all-delivery catalog closure remains a D21 gate.
- Language/framework/model/form/operation catalogs, mapping, README, AGENTS, child/master and cumulative sample are maintained. The historical D1 with-copy rejection is now a D9 positive input-preservation check; its source fixture remains unchanged.

Development failures and their source corrections are retained in [development history](D9-development.md). No failed/incomplete run was committed or marked passed. Publication is automatic under the user's authorization; commit/push verification follows these gates.
