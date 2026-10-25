# Morphir C# source frontend

Convert selected C# source structure to typed Morphir-based IR.
The frontend represents declarations and written operations. It does not simulate CLR execution.

The default output is **Morphir.CSharp.Source v1**, an explicit extension of the Classic model.
A standard Classic v3 reader does not support its source-operation tags.
The separate Classic model codec retains its unchanged v3 format.
See the [source schema](docs/source-structure.md) and [approved change audit](docs/reviews/source-structure-audit.md).

## Current scope

D1–D9 passed the source contract; [validation state](docs/deliveries/source-structure-state.md).
D10–D21 remain planned in the [rewritten delivery plan](Morphir_CSharp_Frontend_delivery_plan.md).

- Source records, classes, structs, fields, properties, constructors, accessors, and operators.
- Blocks, locals, assignments, returns, decisions, constants, and explicit checked/unchecked context.
- Explicit nullability, null/default, nullable access, coalescing, and nullable operators.
- Primitive numeric, Decimal, Half, 128-bit, BigInteger, and Complex source operations.
- Resolved source and metadata calls, written named/optional/params arguments, method references, and scoped local functions.
- Enum initializer expressions, flags operations, tuple labels, deconstruction, and source member updates.
- Actual project references, generators, configuration, additional files, and source locations.

Only explicit nullable types receive Maybe. Nonnullable `string` remains String.
An unguarded `loan.Quantity * loan.Price` becomes two field reads and one Decimal multiplication.
An integer product becomes two field reads and one Basics multiplication.
No null/range/ownership/purity/termination proof or numeric compatibility library is required.
Calls, constructors, accessors, equality, aliases, and cyclic assignments are not executed during translation.

Generics/interfaces/inheritance, general collections, full patterns, anonymous functions, loops/ref forms,
framework data families, object operations, exceptions, iterators, and dynamic retain their delivery gaps.
Unsupported forms, invalid C#, required unselected source, and name collisions fail with source locations.
No partial success artifact is produced.

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
The suite checks the 62 audit cases, source syntax parity, typed products, catalogs, project/generator inputs, and atomic output.
Frozen historical reference hashes, independent Classic codec fixtures, and protected license bytes remain checked.
The former runtime implementation/tests are [archived](docs/history/runtime-contract/README.md).

## CLI

From the repository root, after build:

```bash
dotnet src/Morphir.Frontend.CSharp.Cli/bin/Debug/net10.0/Morphir.Frontend.CSharp.Cli.dll --package Example --namespace Example.CurrentSupported --project samples/CurrentSupportedExample/CurrentSupportedExample.csproj --framework net10.0 --output artifacts/source-structure.json
```

For explicit files, replace the project and framework options with one or more `--source File.cs` options.
Repeat `--namespace` to select additional source namespaces.
Selectors include descendants, unused/private declarations, and selected generated source.
Required outside source must be selected explicitly. Project reference alone does not select a namespace.
`--configuration` applies to project mode. Failed conversion preserves an existing output file.

Core input remains Compilation plus FrontendSettings. Adapters own acquisition and serialization.
External calls retain resolved metadata identities. Optional dependency specifications can map exact targets;
pure/total assertions are not required.

## Samples and catalogs

[CurrentSupportedExample](samples/CurrentSupportedExample/SourceStructure.cs) includes the reported Loan and integer cases.
Its [independent source contract](fixtures/source-structure/contract.json) records expected structure without adopting converter output.
All earlier sample source remains present. Historical Elm references remain frozen and are not source v1 goldens.

- [Language coverage](docs/catalogs/language.md)
- [Framework coverage](docs/catalogs/framework.md)
- [Model and codec coverage](docs/catalogs/model-codec.md)
- [Source contract](docs/source-structure.md)
- [Migration evidence](docs/deliveries/source-structure-state.md)

Reference generation remains a separate reviewed operation:

```bash
npm ci --prefix eng --ignore-scripts --no-audit --no-fund
node eng/generate-references.mjs --write
```

Those commands generate historical Classic references. They do not define source v1 expected output.
Never refresh reference files to conceal a failing check.

## License

Root LICENSE and NOTICE bytes remain unchanged.
See [licensing and supplemental upstream notice](docs/references/LICENSING.md).
