# Morphir C# frontend

Generate standard Morphir Classic v3 from equivalent C# inputs.
Every supported feature has a complete JSON equality test against `morphir-elm make`.
Only whitespace and JSON object-key order are ignored. No semantic normalization is permitted.
See the [acceptance contract](docs/elm-parity.md) and [current validation](docs/deliveries/elm-parity-state.md).

Source v1 was never requested. Its public CLI/API/model/codec paths are removed.
The frontend represents written operations without CLR simulation or runtime safety proofs.
Unmapped forms fail with source locations and leave existing output unchanged.

## Current subset

Exact references cover records/classes/structs, stored fields/properties, positional record constructors,
primitive types, integer/Float/Decimal operations, literals, nullable type annotations, tuples,
initialized locals, conditional expressions, visibility, documentation, calls, recursion, and dependency contracts.
Project tests cover references, generators, additional files, configuration, and CLI output.
The [case registry](fixtures/elm-parity/cases.json) defines the tested subset.
Every supported catalog entry names its exact-parity cases.

D1–D9 retain gaps outside this subset. D10–D21 remain planned.
Mutation, identity/cycles, general construction, explicit accessors/operators, overload naming,
nullable value operations, and other unmapped forms remain rejected until their mapping is approved.

## Build and test

Use SDK 10.0.100, reference pack 10.0.0, C#14, Roslyn 5.0.0, and x64-compatible input.
Integration tests also need Node and the pinned Elm compiler package:

```bash
npm ci --prefix eng --ignore-scripts --no-audit --no-fund
dotnet restore Morphir.CSharp.slnx --packages .packages --locked-mode
dotnet build Morphir.CSharp.slnx --no-restore -m:1
dotnet test Morphir.CSharp.slnx --no-build --no-restore -m:1
python3 eng/check-catalogs.py
python3 eng/check-repository.py
```

VSTest needs a local host socket. Project loading needs local MSBuild.
The suite compares frozen compiler output and runs fresh `morphir-elm make` comparisons.
Tests never rewrite expected JSON. Reference generation is a separate explicit operation:

```bash
node eng/generate-exact-references.mjs --write
```

## CLI

After build, from the repository root:

```bash
dotnet run --project src/Morphir.Frontend.CSharp.Cli --no-build -- --package Example --namespace Example.CurrentSupported --project samples/CurrentSupportedExample/CurrentSupportedExample.csproj --framework net10.0 --output artifacts/current-sample.json
```

Use one or more `--source File.cs` options instead of project/framework options for explicit files.
Repeat `--namespace` for selectors. Selectors include descendants, private code, and unused declarations.
Required outside source must be selected explicitly. `--configuration` applies to project mode.
The core accepts Compilation plus settings; adapters acquire inputs and serialize output.

The current sample includes the supported Loan example and generated source.
Earlier source files remain historical inputs and are excluded from that project.
Historical Elm/codec references remain frozen and do not replace current frontend parity.
See [Classic mappings](docs/classic-output.md), [language](docs/catalogs/language.md),
[framework](docs/catalogs/framework.md), [model/codec](docs/catalogs/model-codec.md), and the [delivery plan](Morphir_CSharp_Frontend_delivery_plan.md).

## License

Root LICENSE and NOTICE bytes remain unchanged.
See [licensing and supplemental upstream notice](docs/references/LICENSING.md).
