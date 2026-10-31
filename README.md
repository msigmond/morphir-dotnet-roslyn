# Morphir C# frontend

Generate standard Morphir IR from C# with output identical to `morphir-elm make` for equivalent Elm inputs.
Represent written operations without hidden CLR simulation or runtime safety proofs.
The [exact Elm parity contract](docs/elm-parity.md) controls all remaining work.
Source v1 was never requested and is not an accepted delivery target.

## Current scope

The current Classic implementation covers records, arithmetic, field reads, initialized locals, returns, and positional calls.
Its mappings and gaps are in [Classic output](docs/classic-output.md).
D1–D9 require exact Elm parity revalidation. Their earlier Source-mode passes do not establish completion.
The existing normalized business comparison is supplemental evidence, not an exact-parity test.
D10–D21 remain planned in the [delivery plan](Morphir_CSharp_Frontend_delivery_plan.md).

Every feature needs paired C#/Elm inputs and full JSON equality against the pinned `morphir-elm make` output.
Ignore only whitespace and JSON object-key order. Do not normalize names, packages, types, arrays, or operations.
If a mapping is unclear, ask the user before implementation. Do not add another output format.

## Prerequisites and checks

Use SDK 10.0.100, .NET reference pack 10.0.0, C#14, Roslyn 5.0.0, and x64 input.
Dependency and compiler pins remain unchanged.

```bash
dotnet restore Morphir.CSharp.slnx --packages .packages --locked-mode
dotnet build Morphir.CSharp.slnx --no-restore -m:1
dotnet test Morphir.CSharp.slnx --no-build --no-restore -m:1
python3 eng/check-catalogs.py
python3 eng/check-repository.py
```

VSTest needs a local host socket. Project loading needs the local MSBuild host.
The existing suite includes legacy Source assertions, typed products, project inputs, normalized comparisons, and atomic output.
These checks do not replace the required exact-parity test for every feature.
Frozen historical reference hashes, independent Classic codec fixtures, and protected license bytes remain checked.
The former runtime implementation/tests are [archived](docs/history/runtime-contract/README.md).

## CLI

From the repository root, after build, for standard Classic output:

```bash
dotnet run --project src/Morphir.Frontend.CSharp.Cli --no-build -- --package Example --namespace Example.CurrentSupported --source samples/CurrentSupportedExample/ClassicLoan.cs --output artifacts/classic-loan.json
```

Use one or more `--source File.cs` options, or `--project App.csproj --framework net10.0`.
The complete cumulative project includes forms outside the current Classic subset. It is not an accepted Classic delivery.
Repeat `--namespace` to select additional source namespaces.
Selectors include descendants, unused/private declarations, and selected generated source.
Required outside source must be selected explicitly. Project reference alone does not select a namespace.
`--configuration` applies to project mode. Failed conversion preserves an existing output file.

Core input remains Compilation plus FrontendSettings. Adapters own acquisition and serialization.
External calls retain resolved metadata identities. Optional dependency specifications can map exact targets;
pure/total assertions are not required.

## Samples and catalogs

[ClassicLoan](samples/CurrentSupportedExample/ClassicLoan.cs) demonstrates default Classic data/call/local mappings.
Earlier source samples and assertions remain historical implementation evidence.
They do not establish exact Elm parity. Frozen historical references remain unchanged.

- [Language coverage](docs/catalogs/language.md)
- [Framework coverage](docs/catalogs/framework.md)
- [Model and codec coverage](docs/catalogs/model-codec.md)
- [Exact Elm parity contract](docs/elm-parity.md)
- [Source migration evidence](docs/deliveries/source-structure-state.md)
- [Classic correction evidence](docs/deliveries/classic-output-state.md)

Reference generation remains a separate reviewed operation:

```bash
npm ci --prefix eng --ignore-scripts --no-audit --no-fund
node eng/generate-references.mjs --write
```

Those commands generate historical Classic references. Every new feature also needs its own reproducible Elm compiler reference.
Never refresh reference files to conceal a failing check.

## License

Root LICENSE and NOTICE bytes remain unchanged.
See [licensing and supplemental upstream notice](docs/references/LICENSING.md).
