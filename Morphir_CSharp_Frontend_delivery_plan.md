# Morphir C# frontend — source structure delivery plan

- authority: user instructions → approved source audit → this plan → selected delivery
- contract changed: 2026-10-24; [approved audit](docs/reviews/source-structure-audit.md)
- D1–D9 passed under the source contract on 2026-10-24. D10–D21 remain planned.
- former runtime evidence: [archive](docs/history/runtime-contract/README.md)
- default output: standard Classic v3; explicit `--format source` retains the C# extension; [current correction](docs/deliveries/classic-output-state.md)
- current source-mode contract: [source schema and mappings](docs/source-structure.md)

## Contract

Represent C# declarations and operations as written. Do not simulate CLR execution.
Use Maybe only for an explicit nullable type. Use declared method result types.
Keep explicit checks, defaults, casts, calls, assignments, branches, and source order.
Do not add safety proofs, hidden initialization, numeric emulation, ownership state, or synthesized bodies.
Preserve source type distinctions, spelling, modifiers, labels, and locations in source metadata.
Default output uses the [documented Classic mappings](docs/classic-output.md).
Source mode preserves the complete syntax contract below. Its envelope is `Morphir.CSharp.Source` version 1.
Source-operation tags are extensions. They are not standard Classic v3 JSON.
The separate plain Classic model codec retains format version 3.

## Execution

Work on one delivery unless the user authorizes a contract migration across deliveries.
Check prerequisite evidence. A planned delivery is not implemented by documentation alone.
For each change: update mapping, implement structure, add independent assertions, run checks, update evidence.
Record coverage for both output modes. Source-mode coverage alone does not establish Classic support.
For each planned form, specify a Classic mapping or an explicit Classic rejection before implementation.
No behavior or safety proof is a substitute for a source mapping.
No selected unsupported declaration may be silently omitted.
Compiler errors, required unselected source, identity collisions, and unmapped forms fail with source locations.
Failure creates no partial success artifact and does not overwrite existing output.

## Baseline and layout

Keep SDK 10.0.100, .NET reference pack 10.0.0, Roslyn 5.0.0, C#14, and explicit x64 CLI input.
Keep `Morphir.CSharp.slnx`, Classic/Json/core/ProjectSystem/CLI projects, tests, fixtures, samples, and eng.
Core input remains Compilation plus settings. Adapters own acquisition and serialization.
Keep actual project references, compiler options, generators, AdditionalFiles, and analyzer configuration.
Namespace selectors use complete case-sensitive segments and include descendants, private and unused declarations.
Types belong to namespace/enclosing-type modules. Members retain their source owner.
Normalize Morphir names for lookup. Preserve original names and declaration order in source metadata.
Keep Public/Private projection and exact source modifiers in metadata.
Keep pinned primitive/framework families. Future forms need their explicit structural mappings.

## Deliveries

| Delivery | Source capability | State | Prerequisites |
| --- | --- | --- | --- |
| [D1](Morphir_CSharp_Frontend_deliveries/D01-cli-record.md) | CLI, source data, typed output | passed; source contract | — |
| [D2](Morphir_CSharp_Frontend_deliveries/D02-project-inputs.md) | Project and generated source inputs | passed; source contract | [D1](Morphir_CSharp_Frontend_deliveries/D01-cli-record.md) |
| [D3](Morphir_CSharp_Frontend_deliveries/D03-blocks-locals.md) | Blocks, locals, and statements | passed; source contract | [D2](Morphir_CSharp_Frontend_deliveries/D02-project-inputs.md) |
| [D4](Morphir_CSharp_Frontend_deliveries/D04-maybe.md) | Explicit nullability and nullable syntax | passed; source contract | [D3](Morphir_CSharp_Frontend_deliveries/D03-blocks-locals.md) |
| [D5](Morphir_CSharp_Frontend_deliveries/D05-numerics.md) | Numeric source structure | passed; source contract | [D3](Morphir_CSharp_Frontend_deliveries/D03-blocks-locals.md) |
| [D6](Morphir_CSharp_Frontend_deliveries/D06-decimal.md) | Decimal source structure | passed; source contract | [D4](Morphir_CSharp_Frontend_deliveries/D04-maybe.md), [D5](Morphir_CSharp_Frontend_deliveries/D05-numerics.md) |
| [D7](Morphir_CSharp_Frontend_deliveries/D07-calls.md) | Calls, references, and arguments | passed; source contract | [D2](Morphir_CSharp_Frontend_deliveries/D02-project-inputs.md), [D3](Morphir_CSharp_Frontend_deliveries/D03-blocks-locals.md), [D4](Morphir_CSharp_Frontend_deliveries/D04-maybe.md), [D5](Morphir_CSharp_Frontend_deliveries/D05-numerics.md), [D6](Morphir_CSharp_Frontend_deliveries/D06-decimal.md) |
| [D8](Morphir_CSharp_Frontend_deliveries/D08-enums-tuples.md) | Enum and tuple source structure | passed; source contract | [D5](Morphir_CSharp_Frontend_deliveries/D05-numerics.md), [D7](Morphir_CSharp_Frontend_deliveries/D07-calls.md) |
| [D9](Morphir_CSharp_Frontend_deliveries/D09-user-data.md) | Data declarations, members, and state | passed; source contract | [D7](Morphir_CSharp_Frontend_deliveries/D07-calls.md), [D8](Morphir_CSharp_Frontend_deliveries/D08-enums-tuples.md) |
| [D10](Morphir_CSharp_Frontend_deliveries/D10-generics-dispatch.md) | Generics, interfaces, and inheritance | planned | [D9](Morphir_CSharp_Frontend_deliveries/D09-user-data.md) |
| [D11](Morphir_CSharp_Frontend_deliveries/D11-collections.md) | Arrays and collection structure | planned | [D4](Morphir_CSharp_Frontend_deliveries/D04-maybe.md), [D10](Morphir_CSharp_Frontend_deliveries/D10-generics-dispatch.md) |
| [D12](Morphir_CSharp_Frontend_deliveries/D12-patterns.md) | Patterns and switches | planned | [D8](Morphir_CSharp_Frontend_deliveries/D08-enums-tuples.md), [D9](Morphir_CSharp_Frontend_deliveries/D09-user-data.md), [D10](Morphir_CSharp_Frontend_deliveries/D10-generics-dispatch.md), [D11](Morphir_CSharp_Frontend_deliveries/D11-collections.md) |
| [D13](Morphir_CSharp_Frontend_deliveries/D13-closures.md) | Anonymous functions and delegates | planned | [D7](Morphir_CSharp_Frontend_deliveries/D07-calls.md), [D10](Morphir_CSharp_Frontend_deliveries/D10-generics-dispatch.md) |
| [D14](Morphir_CSharp_Frontend_deliveries/D14-recursion.md) | Recursive binding coverage | planned | [D7](Morphir_CSharp_Frontend_deliveries/D07-calls.md), [D13](Morphir_CSharp_Frontend_deliveries/D13-closures.md) |
| [D15](Morphir_CSharp_Frontend_deliveries/D15-loops-local-state.md) | Loops, compound updates, and references | planned | [D11](Morphir_CSharp_Frontend_deliveries/D11-collections.md), [D14](Morphir_CSharp_Frontend_deliveries/D14-recursion.md) |
| [D16](Morphir_CSharp_Frontend_deliveries/D16-framework-data.md) | Framework data structure | planned | [D6](Morphir_CSharp_Frontend_deliveries/D06-decimal.md), [D11](Morphir_CSharp_Frontend_deliveries/D11-collections.md) |
| [D17](Morphir_CSharp_Frontend_deliveries/D17-type-closure.md) | Object and remaining type forms | planned | [D8](Morphir_CSharp_Frontend_deliveries/D08-enums-tuples.md), [D9](Morphir_CSharp_Frontend_deliveries/D09-user-data.md), [D10](Morphir_CSharp_Frontend_deliveries/D10-generics-dispatch.md), [D11](Morphir_CSharp_Frontend_deliveries/D11-collections.md), [D12](Morphir_CSharp_Frontend_deliveries/D12-patterns.md), [D13](Morphir_CSharp_Frontend_deliveries/D13-closures.md), [D14](Morphir_CSharp_Frontend_deliveries/D14-recursion.md), [D15](Morphir_CSharp_Frontend_deliveries/D15-loops-local-state.md), [D16](Morphir_CSharp_Frontend_deliveries/D16-framework-data.md) |
| [D18.1](Morphir_CSharp_Frontend_deliveries/D18_1-caught-failures.md) | Exception syntax | planned | [D3](Morphir_CSharp_Frontend_deliveries/D03-blocks-locals.md), [D4](Morphir_CSharp_Frontend_deliveries/D04-maybe.md), [D5](Morphir_CSharp_Frontend_deliveries/D05-numerics.md), [D6](Morphir_CSharp_Frontend_deliveries/D06-decimal.md), [D7](Morphir_CSharp_Frontend_deliveries/D07-calls.md), [D8](Morphir_CSharp_Frontend_deliveries/D08-enums-tuples.md), [D9](Morphir_CSharp_Frontend_deliveries/D09-user-data.md), [D10](Morphir_CSharp_Frontend_deliveries/D10-generics-dispatch.md), [D11](Morphir_CSharp_Frontend_deliveries/D11-collections.md), [D12](Morphir_CSharp_Frontend_deliveries/D12-patterns.md), [D13](Morphir_CSharp_Frontend_deliveries/D13-closures.md), [D14](Morphir_CSharp_Frontend_deliveries/D14-recursion.md), [D15](Morphir_CSharp_Frontend_deliveries/D15-loops-local-state.md), [D16](Morphir_CSharp_Frontend_deliveries/D16-framework-data.md), [D17](Morphir_CSharp_Frontend_deliveries/D17-type-closure.md) |
| [D18.2](Morphir_CSharp_Frontend_deliveries/D18_2-lazy-iterators.md) | Iterator syntax | planned | [D18.1](Morphir_CSharp_Frontend_deliveries/D18_1-caught-failures.md), [D11](Morphir_CSharp_Frontend_deliveries/D11-collections.md), [D12](Morphir_CSharp_Frontend_deliveries/D12-patterns.md), [D13](Morphir_CSharp_Frontend_deliveries/D13-closures.md), [D14](Morphir_CSharp_Frontend_deliveries/D14-recursion.md), [D15](Morphir_CSharp_Frontend_deliveries/D15-loops-local-state.md) |
| [D18.3](Morphir_CSharp_Frontend_deliveries/D18_3-dynamic.md) | Dynamic syntax | planned | [D18.2](Morphir_CSharp_Frontend_deliveries/D18_2-lazy-iterators.md), [D7](Morphir_CSharp_Frontend_deliveries/D07-calls.md), [D10](Morphir_CSharp_Frontend_deliveries/D10-generics-dispatch.md), [D17](Morphir_CSharp_Frontend_deliveries/D17-type-closure.md) |
| [D18.4](Morphir_CSharp_Frontend_deliveries/D18_4-feature-closure.md) | Feature and rejection closure | planned | [D18.1](Morphir_CSharp_Frontend_deliveries/D18_1-caught-failures.md), [D18.2](Morphir_CSharp_Frontend_deliveries/D18_2-lazy-iterators.md), [D18.3](Morphir_CSharp_Frontend_deliveries/D18_3-dynamic.md) |
| [D19](Morphir_CSharp_Frontend_deliveries/D19-classic-complete.md) | Complete models and codecs | planned | [D18.4](Morphir_CSharp_Frontend_deliveries/D18_4-feature-closure.md) |
| [D20](Morphir_CSharp_Frontend_deliveries/D20-msbuild.md) | MSBuild adapter | planned | [D19](Morphir_CSharp_Frontend_deliveries/D19-classic-complete.md), [D2](Morphir_CSharp_Frontend_deliveries/D02-project-inputs.md) |
| [D21](Morphir_CSharp_Frontend_deliveries/D21-acceptance.md) | Complete acceptance | planned | [D20](Morphir_CSharp_Frontend_deliveries/D20-msbuild.md) |

## Validation

Run restore in locked mode, single-worker build, and the full test suite.
Use source shape and declared types as expected behavior. Do not compare CLR execution results.
Retain independent Classic model/codec references and all frozen historical fixture hashes.
New source references must be authored independently and reviewed separately from validation.
Do not generate expected output from the converter to repair a failing check.
Update CurrentSupportedExample, catalogs, mappings, README, AGENTS, delivery states, and source schema together.
Preserve root LICENSE/NOTICE bytes and the CLAUDE pointer.
The repository checker verifies all 24 plans and archived paired code, links, hashes, licenses, and whitespace.
D21 requires all admitted catalog entries complete; interim catalog checks retain pending obligations.

## Commands

```bash
dotnet restore Morphir.CSharp.slnx --packages .packages --locked-mode
dotnet build Morphir.CSharp.slnx --no-restore -m:1
dotnet test Morphir.CSharp.slnx --no-build --no-restore -m:1
python3 eng/check-catalogs.py
python3 eng/check-repository.py
```

VSTest needs a local host socket. Reference generation is a separate reviewed operation.
