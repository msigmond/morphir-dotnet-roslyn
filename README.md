# Morphir .NET Roslyn

C# data and pure business logic → typed Morphir Classic IR, format version 3.

D1 supplies an explicit-source-file CLI, a Roslyn frontend, a Roslyn-free typed model and explicit JSON codecs. D2 adds actual project inputs, explicit generator execution and qualified source-reference calls; D3 adds blocks, locals/reassignment and decisions. D1–D7 are passed; D8 onward remains pending. See the [master plan](Morphir_CSharp_Frontend_delivery_plan.md), [D1 contract](Morphir_CSharp_Frontend_deliveries/D01-cli-record.md), and [D1 evidence](docs/deliveries/D1-state.md) and [D2 state](docs/deliveries/D2-state.md).

## Prerequisites

- .NET SDK **10.0.100** (`global.json`, roll-forward disabled), C# **14**, .NET reference pack **10.0.0** (installed by that SDK).
- Roslyn/compiler/workspaces **5.0.0**, MSBuild Locator **1.10.12**, xUnit **2.9.3**, runner **3.0.2**, test SDK **17.14.1**; exact central pins and per-project package locks.
- For reference generation only: Node.js/npm (verified with **20.18.1/9.2.0**), Git, Morphir Elm **2.100.0**, Elm **0.19.1**. Tool packages and transitive integrity hashes are locked in `eng/package-lock.json`; Morphir source revision is `71ca7ee385273b0af6f8db66582c26e9b410cbf2`.
- Python 3 for repository contract checks. Package acquisition requires network access; normal validation uses committed reference JSON.

## Build and validate

Run from this repository root:

```sh
dotnet restore Morphir.CSharp.slnx --packages .packages --locked-mode
dotnet build Morphir.CSharp.slnx --no-restore -m:1
dotnet test Morphir.CSharp.slnx --no-build --no-restore -m:1 --filter 'Delivery=D1|Delivery=D2|Delivery=D3|Delivery=D4|Delivery=D5|Delivery=D6|Delivery=D7'
python3 eng/check-repository.py
python3 eng/check-catalogs.py
```

The solution includes the cumulative sample. Tests compare complete distributions with independent Elm outputs, check compiled C#/IR behavior at boundaries, exercise diagnostics and the CLI, and verify reference hashes. Expected JSON is never rewritten by build/test/check commands. `python3 eng/check-catalogs.py --require-complete` is the final-scope gate and intentionally rejects the pending later feature and pure-member closure obligations; it is not an intermediate delivery completion requirement. VSTest requires a local socket to communicate with its host; restrictive execution sandboxes may need permission for the test command.

## CLI

```sh
dotnet run --project src/Morphir.Frontend.CSharp.Cli --no-build -- \
  --package Example --namespace Example.CurrentSupported \
  --source samples/CurrentSupportedExample/CurrentSupportedExample.cs \
  --output artifacts/current-supported.json
```

Project mode builds against restored assets and runs configured generators with the evaluated inputs:

```sh
dotnet run --project src/Morphir.Frontend.CSharp.Cli --no-build -- \
  --package Example --namespace Example.CurrentSupported \
  --project samples/CurrentSupportedExample/CurrentSupportedExample.csproj \
  --framework net10.0 --configuration Debug --output artifacts/current-supported-project.json
```

Select `Example.D2.Business` and `Example.D2.Shared` explicitly for the sibling namespaces in the D2 fixture graph. Repeat `--source` for more files and `--namespace` for additional selectors. Package, source inputs, selectors and output are explicit. Exit status: 0 success, 1 compiler/conversion rejection, 2 invocation/I/O failure. Diagnostics include a stable code and source location. Successful writes use deterministic UTF-8 and atomic replacement; a rejected conversion leaves an existing artifact intact and creates no new artifact.

## Current support and boundaries

D1 supports positional **readonly record structs** with `int`/`bool` data, static expression-bodied functions, record construction/reads, Int32 comparisons, Boolean `&&`/`||`, literal strings, and proven-safe Int32 addition. Addition requires a constant sum within Int32 range or a zero operand. D5 extends this baseline with exact unchecked arithmetic for all fixed primitive numeric widths, native64, Half, Int128/UInt128, BigInteger and Complex; wide values use exact limbs and floating precision/nonfinite values use complete compatibility helpers. D4 adds nullable int/bool and String reference slots, guarded extraction, defaults, coalescing/conditional access and lifted operations; D6 adds exact Decimal construction, coefficient/scale/sign, guarded arithmetic/conversions and rounding; D7 adds resolved named/optional/params calls, record receivers, noncapturing local functions and pure method references; general data/dispatch/capture forms remain D9/D10/D13; D3 admits blocks, locals/constants, local/by-value parameter rebinding, if/else, early returns, conditional expressions and Boolean operations with source-order evaluation. D2 currently admits exact nonrecursive selected static positional by-value calls.

Namespace selectors match complete, case-sensitive segments and include descendants. Overlaps are deduplicated. Unused/nonpublic declarations are included; their `Public`/`Private` wrappers and source owner paths are retained. A required source type outside the selected namespaces produces a diagnostic until its namespace is explicitly selected. Unsupported selected types/members/operations fail the whole conversion. Generated record plumbing is represented by the data mapping rather than exported as synthetic methods.

Checked expressions/blocks/operators and checked semantic arithmetic/conversions, async/await, pointer/ref-like uses, identity, effects, input/external mutation and forms outside the delivered contract are rejected. Division/remainder require source safety proofs; BigInteger narrowing requires target bounds, floating-to-BigInteger construction requires finiteness, and Complex-to-BigInteger additionally requires zero imaginary component. No presence/default/error wrapper is invented. The CLI fixes the native target to x64; the compilation API rejects incompatible 32-bit settings. Full exclusion and later-delivery policies remain in the master plan.

The [CurrentSupportedExample](samples/CurrentSupportedExample/CurrentSupportedExample.cs) exercises delivered forms. Its [language coverage map](docs/catalogs/language.md), [semantic mapping](docs/mappings/D1.md), [framework inventory](docs/catalogs/framework.md), and [model/codec inventory](docs/catalogs/model-codec.md) describe the precise support boundary.

## Independent references

References are committed under `fixtures/D1/expected`; compiler mode, source and output hashes are in [provenance.json](fixtures/D1/expected/provenance.json). Ordinary Elm fixtures use the default compiler. Private cross-module data, cumulative combined cases and model-only dependencies use a complete Elm algebra/codec host against the unmodified pinned source; no compiled JSON is patched and no private declaration is promoted.

Reference generation is a separate, explicit maintenance operation, followed by review of the changed Elm sources, JSON and provenance:

```sh
npm ci --prefix eng --ignore-scripts --no-audit --no-fund
node eng/generate-references.mjs --write
# Separate D2 generation; leaves all D1 reference sources and JSON unchanged.
node eng/generate-d2-references.mjs --write
# Separate D3 generation, retaining D1/D2 references.
node eng/generate-d3-references.mjs --write
# Separate D4 generation; prior references remain frozen.
node eng/generate-d4-references.mjs --write
# Separate D5 generation; complete original helpers and independent codec host.
node eng/generate-d5-references.mjs --write
# Separate D6 generation; exact Decimal helper/source/sample/model references.
node eng/generate-d6-references.mjs --write
# Separate D7 generation; canonical calls, callable adapters and external specifications.
node eng/generate-d7-references.mjs --write
```

Do not run reference generation during validation or to make a failing parity test pass. Tests and `eng/check-repository.py` check the frozen hashes.

## Layout

- `src/Morphir.IR.Classic`: generic attribute algebra, typed distribution containers and public specifications.
- `src/Morphir.IR.Json`: explicit v3 codecs, structural comparator and atomic output.
- `src/Morphir.Frontend.CSharp`: Compilation + settings → typed distribution + diagnostics; explicit-file reference acquisition.
- `src/Morphir.Frontend.CSharp.ProjectSystem`: actual MSBuild project graph, build verification, explicit generator pass, input provenance.
- `src/Morphir.Frontend.CSharp.Cli`: file/project command-line adapter.
- `tests`: model, JSON, frontend and integration suites, tagged by owning delivery (`Delivery=D1` through the active delivery).
- `samples`, `fixtures`, `eng`, `docs`: cumulative source, independent references, tooling, mappings/catalogs/evidence.

The MSBuild task adapter (D20) and complete Classic algebra coverage (D19) remain later deliveries. Generator tooling targets netstandard2.0 as required by Roslyn; business/frontend compilation remains .NET 10/C#14. [Historical documents](docs/history/) and the [archived prototype](docs/references/prototype-addition/PROVENANCE.md) are context, not current commands or support claims.

## License and review

[Apache License 2.0](LICENSE); [NOTICE](NOTICE); [licensing reconciliation and complete upstream attribution](docs/references/LICENSING.md). Root LICENSE/NOTICE bytes are preserved. The user authorized automatic commit/push after each fully passed D2–D10 delivery. Failed/incomplete deliveries are not committed automatically. [AGENTS.md](AGENTS.md) defines repository workflow.

D3 contracts and evidence: [mapping](docs/mappings/D3.md), [state](docs/deliveries/D3-state.md). [Blocks.cs](samples/CurrentSupportedExample/Blocks.cs) extends the cumulative project; [reference.json](samples/CurrentSupportedExample/reference.json) identifies its frozen complete independent distribution. The original standalone D1 file/reference remains available for regression.

D4 M1 mapping and current evidence: [mapping](docs/mappings/D4.md), [state](docs/deliveries/D4-state.md), [cumulative presence example](samples/CurrentSupportedExample/Presence.cs).

D5 numeric contracts and evidence: [mapping](docs/mappings/D5.md), [state](docs/deliveries/D5-state.md), [all-family cumulative example](samples/CurrentSupportedExample/Numeric.cs), [exact public member inventory](docs/catalogs/numeric-members.json). Type/operator support does not imply complete numeric framework API support; pending pure-member closure and later prerequisites remain explicit in the catalogs.

Production helper generation is separate from reference generation and normal builds: `node eng/build-numeric-compat.mjs --write` compiles the reviewed original pure Elm and emits complete Classic constructors, with [hash provenance](docs/references/numeric-compat-provenance.json). `node eng/check-numeric-compat.mjs` performs read-only arithmetic/JSON transport checks against independent native JS BigInt. `sh eng/numeric-inventory/generate.sh --write` explicitly maintains the exact pinned public-symbol inventory; validation only compares its saved entries. Never use these generation commands to adopt frontend output or repair a failing parity test.

D6 Decimal contracts and gate evidence: [mapping](docs/mappings/D6.md), [state](docs/deliveries/D6-state.md), [cumulative example](samples/CurrentSupportedExample/Decimal.cs), [exact public symbol inventory](docs/catalogs/decimal-members.json). Exact SDK construction uses a proved Just-only extraction with no fallback. Arithmetic, conversion and member failures require source proofs; a nonzero divisor alone does not prove quotient safety. Intrinsic Decimal arithmetic follows D3 safety even with compilation overflow checks enabled. Binary-floating Decimal construction remains an explicit rejection.

`node eng/build-decimal-compat.mjs --write` explicitly builds the reviewed original Decimal library and frozen typed factory; `node eng/check-decimal-compat.mjs` compares compiled original Elm with fresh native CLR behavior cases without touching expected Morphir JSON. `sh eng/decimal-inventory/generate.sh --write` explicitly maintains the pinned public-symbol catalog. These generation operations remain separate from build/test/parity validation.

D7 call contracts and gate evidence: [mapping](docs/mappings/D7.md), [state](docs/deliveries/D7-state.md), [cumulative calls](samples/CurrentSupportedExample/Calls.cs). The core API accepts explicit reviewed pure/total external contracts and complete dependency specifications; the CLI never infers DLL purity. Params vectors preserve null/empty distinctions and require presence/bounds proofs. General arrays and captured/delegate forms keep D11/D13 prerequisites.
