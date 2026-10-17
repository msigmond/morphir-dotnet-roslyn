# D2 — execution evidence

- Status: passed; prerequisite D1 passed at commit bd4c33f.
- Authorization: user requested D2–D10 sequentially, with automatic commit/push after each passed delivery. Do not start later deliveries before D2 gates pass.
- Resume follow-up: hourly thread heartbeat `continue-morphir-d2-d10`; resume after five-hour capacity reset when necessary, without spending reset credits or changing billing.
- Implemented: actual project inputs; explicit incremental/legacy generator driver pass; complete source-project compilation graph; qualified selected static positional calls; private-inclusive lookup; exhaustive owner/state catalogs; four independent complete source/codec references; cumulative project-mode sample.
- Required remaining work: no D2 implementation/gate work remains. Commit and push are authorized and performed after this evidence update. Expected source/codec references were frozen during final validation.

## Implementation / proof decisions

- New `Morphir.Frontend.CSharp.ProjectSystem` project uses Roslyn compiler/workspaces 5.0.0 and MSBuild Locator 1.10.12 exact pins. It verifies SDK 10.0.100, selected net10.0 build, actual evaluated options/references/configuration, then runs an explicit generator pass for every source project. CompilationReferences preserve source symbols, owning semantic models and project boundaries.
- Builds do not restore or upgrade dependencies. Generator tooling targets netstandard2.0 as required by Roslyn compiler-extension diagnostics; frontend/business stay .NET10/C#14. Incremental generators receive actual AdditionalFiles and CompilerVisibleProperty/analyzer config; driver exceptions/load failures are fatal. Project input provenance includes hashes and identities outside business IR.
- D2 introduces the minimal selected-static positional call contract required by its cross-container seeds. Extended named/default/params/ref/generic/dispatch/recursive contracts remain pending in their owners. Unknown external calls, unselected required namespaces, recursive cycles and unsupported/impure callee bodies fail whole conversion. Source access is checked by C#; IR retains true Private wrappers and private-inclusive lookup.
- Complete `calls` ordinary Elm/default reference plus `project`, `internal`, `sample` independent complete codec distributions; D1 references are unchanged. Private cross-module definitions were constructed using the pinned Elm algebra/codec, never by promoting private access or editing compiled JSON.
- Exhaustive fixed forms, accepted types and all 131 named Roslyn OperationKind entries (including aliases/non-C# markers) have owner/state/domain records. Pending member/representation/proof/fixture obligations remain final-release gates; no fixed source type/member scope is dropped.
- Development validation caught reused background MSBuild workers keeping redirected output streams open. Child builds/fixture restores now disable node reuse. Source-located diagnostic lists are sorted deterministically.

- Final access audit: newly supported internal type-owned modules carry Private module wrappers as well as Private values; namespace grouping modules remain Public. The independent D2 codec host constructs these exact wrappers, and private-inclusive lookup resolves qualified references without promoting modules or declarations.

## Final verification

- `dotnet restore Morphir.CSharp.slnx --packages .packages --locked-mode` — passed, all 12 projects.
- `dotnet restore fixtures/D2/projects/Business/Business.csproj --packages .packages --locked-mode -m:1` — passed, all 3 fixture projects.
- `dotnet build Morphir.CSharp.slnx --no-restore -m:1` — passed, 0 warnings / 0 errors after final access/SDK/input guards.
- `dotnet test Morphir.CSharp.slnx --no-build --no-restore -m:1 --filter 'Delivery=D1|Delivery=D2' --logger 'trx;LogFilePrefix=verified-D2' --results-directory artifacts/test-results` — passed: **68 tests, 0 failed / 0 skipped** (12 integration, 35 frontend/catalog, 4 Classic, 17 Json). Reports: ignored `artifacts/test-results/verified-D2_*.trx`.
- `python3 eng/check-repository.py` — passed; all frozen D1/D2 reference hashes, original 24 child plan code blocks, Markdown links/fences, protected license bytes, CLAUDE pointer and tracked/new/staged whitespace.

The automatic-resume heartbeat is attached to this chat. Future delivery work must read this state, finish any failed required gate, and respect the existing user authorization to commit/push each passed delivery D2–D10. Never mark unfinished work passed to advance the sequence.

- `python3 eng/check-catalogs.py` — passed: 294 owner/state entries. `--require-complete` correctly exits 1 with 271 future/rejection-evidence/member/fixture obligations outstanding; this blocks premature D21 acceptance and is expected in D2.
- No golden/source/provenance refresh occurred during final verification. The final Private module correction was independently authored in Elm and generated before final validation; D1 sources/JSON/provenance remain unchanged.
- Actual cumulative project mode compiles/transpiles and matches the complete independent D2 sample; separate-assembly C#/IR boundary behavior and project-mode CLI failure preservation passed.
- Root LICENSE/NOTICE and CLAUDE pointer bytes are unchanged. All 24 plans/paired embedded code and Markdown links/fences pass. Subsequent work: D3, then remaining requested deliveries through D10, one passed/committed/pushed delivery at a time.
