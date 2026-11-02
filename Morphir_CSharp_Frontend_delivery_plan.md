# Morphir C# frontend — Elm parity delivery plan

- authority: user instructions → [exact Elm parity](docs/elm-parity.md) → approved source-structure rules → this plan → selected delivery
- contract changed: 2026-10-24; [approved audit](docs/reviews/source-structure-audit.md)
- D1–D13 and the [D11 follow-up](Morphir_CSharp_Frontend_deliveries/D11-collections.md#collection-operation-follow-up) have implemented [approved Elm mappings and tested rejections](docs/deliveries/D11-follow-up-elm-parity.md); all gates passed. D14–D21 remain planned.
- former runtime evidence: [archive](docs/history/runtime-contract/README.md)
- requested output: standard Classic v3 matching `morphir-elm make`; [acceptance contract](docs/elm-parity.md)
- Source v1 was unrequested work. Public Source output is removed.

## Contract

Represent C# declarations and operations as written. Do not simulate CLR execution.
Keep declared types. Explicit nullable declarations use Maybe; approved Maybe operations use typed constructors/calls. Infer local types from written expressions, without flow proofs.
Keep written operations through the approved Elm mapping. Named arguments use parameter order; omitted defaults and params lists follow the approved call mapping. Unmapped assignments remain rejected.
Seeded Aggregate uses the [approved two-parameter adapter](docs/mappings/D11.md#collection-operation-follow-up) for Elm callback order.
Do not add safety proofs, hidden initialization, numeric emulation, ownership state, or synthesized bodies.
Use the agreed Elm mapping and standard Classic nodes.
Do not introduce a source envelope or custom tags when a form has no confirmed mapping.
Source v1 code and checks do not establish delivery acceptance.

## Execution

Continue only through approved mappings and exact compiler parity.
D10 uses the [approved generic profile](docs/mappings/D10.md).
All [six mapping decisions](docs/reviews/d1-d9-elm-mapping-decisions.md) are approved.
Source-mode passes do not establish a delivery gate.

Work on one requested delivery. Check prerequisite exact-parity evidence before implementation.
Every feature must pass the [exact Elm parity gate](docs/elm-parity.md).
Use equivalent C#/Elm inputs and expected JSON from the pinned `morphir-elm make` command.
Compare the complete parsed JSON. Ignore only whitespace and object-key order.
Do not normalize packages, names, types, array order, or operations to make a test pass.
Source assertions, normalized comparisons, codec-only fixtures, and rejection tests do not replace this gate.
If an Elm counterpart or mapping is unclear, ask the user. Do not add a custom output format.
Keep source-located compiler, selection, collision, and unsupported-form failures.
Failure must not produce partial output or overwrite an existing output file.

## Baseline and layout

Keep SDK 10.0.100, .NET reference pack 10.0.0, Roslyn 5.0.0, C#14, and explicit x64 CLI input.
Keep `Morphir.CSharp.slnx`, Classic/Json/core/ProjectSystem/CLI projects, tests, fixtures, samples, and eng.
Core input remains Compilation plus settings. Adapters own acquisition and serialization.
Keep actual project references, compiler options, generators, AdditionalFiles, and analyzer configuration.
Namespace selectors use complete case-sensitive segments and include descendants, private and unused declarations.
Types belong to namespace/enclosing-type modules. Members retain their source owner.
Use matched names and owners in paired fixtures. Keep Public/Private projection in standard IR.
Keep pinned primitive/framework families. Future forms need their explicit structural mappings.

## Deliveries

| Delivery | Source capability | State | Prerequisites |
| --- | --- | --- | --- |
| [D1](Morphir_CSharp_Frontend_deliveries/D01-cli-record.md) | CLI, source data, typed output | passed under the approved Elm profile | — |
| [D2](Morphir_CSharp_Frontend_deliveries/D02-project-inputs.md) | Project and generated source inputs | passed under the approved Elm profile | [D1](Morphir_CSharp_Frontend_deliveries/D01-cli-record.md) |
| [D3](Morphir_CSharp_Frontend_deliveries/D03-blocks-locals.md) | Blocks, locals, and statements | passed under the approved Elm profile | [D2](Morphir_CSharp_Frontend_deliveries/D02-project-inputs.md) |
| [D4](Morphir_CSharp_Frontend_deliveries/D04-maybe.md) | Explicit nullability and nullable syntax | passed under the approved Elm profile | [D3](Morphir_CSharp_Frontend_deliveries/D03-blocks-locals.md) |
| [D5](Morphir_CSharp_Frontend_deliveries/D05-numerics.md) | Numeric source structure | passed under the approved Elm profile | [D3](Morphir_CSharp_Frontend_deliveries/D03-blocks-locals.md) |
| [D6](Morphir_CSharp_Frontend_deliveries/D06-decimal.md) | Decimal source structure | passed under the approved Elm profile | [D4](Morphir_CSharp_Frontend_deliveries/D04-maybe.md), [D5](Morphir_CSharp_Frontend_deliveries/D05-numerics.md) |
| [D7](Morphir_CSharp_Frontend_deliveries/D07-calls.md) | Calls, references, and arguments | passed under the approved Elm profile | [D2](Morphir_CSharp_Frontend_deliveries/D02-project-inputs.md), [D3](Morphir_CSharp_Frontend_deliveries/D03-blocks-locals.md), [D4](Morphir_CSharp_Frontend_deliveries/D04-maybe.md), [D5](Morphir_CSharp_Frontend_deliveries/D05-numerics.md), [D6](Morphir_CSharp_Frontend_deliveries/D06-decimal.md) |
| [D8](Morphir_CSharp_Frontend_deliveries/D08-enums-tuples.md) | Enum and tuple source structure | passed under the approved Elm profile | [D5](Morphir_CSharp_Frontend_deliveries/D05-numerics.md), [D7](Morphir_CSharp_Frontend_deliveries/D07-calls.md) |
| [D9](Morphir_CSharp_Frontend_deliveries/D09-user-data.md) | Data declarations, members, and state | passed under the approved Elm profile | [D7](Morphir_CSharp_Frontend_deliveries/D07-calls.md), [D8](Morphir_CSharp_Frontend_deliveries/D08-enums-tuples.md) |
| [D10](Morphir_CSharp_Frontend_deliveries/D10-generics-dispatch.md) | Generic Elm profile and explicit dispatch rejections | passed: approved profile | [D9](Morphir_CSharp_Frontend_deliveries/D09-user-data.md) |
| [D11](Morphir_CSharp_Frontend_deliveries/D11-collections.md) | Approved collection profile | passed: approved profile | [D4](Morphir_CSharp_Frontend_deliveries/D04-maybe.md), [D10](Morphir_CSharp_Frontend_deliveries/D10-generics-dispatch.md) |
| [D12](Morphir_CSharp_Frontend_deliveries/D12-patterns.md) | Direct case profile and explicit pattern rejections | passed: approved profile | [D8](Morphir_CSharp_Frontend_deliveries/D08-enums-tuples.md), [D9](Morphir_CSharp_Frontend_deliveries/D09-user-data.md), [D10](Morphir_CSharp_Frontend_deliveries/D10-generics-dispatch.md), [D11](Morphir_CSharp_Frontend_deliveries/D11-collections.md) |
| [D13](Morphir_CSharp_Frontend_deliveries/D13-closures.md) | Anonymous functions and delegates | passed: approved profile | [D7](Morphir_CSharp_Frontend_deliveries/D07-calls.md), [D10](Morphir_CSharp_Frontend_deliveries/D10-generics-dispatch.md) |
| [D11 follow-up](Morphir_CSharp_Frontend_deliveries/D11-collections.md#collection-operation-follow-up) | Collection filter, map, and fold operations | passed: approved profile | [D11](Morphir_CSharp_Frontend_deliveries/D11-collections.md), [D13](Morphir_CSharp_Frontend_deliveries/D13-closures.md) |
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

Every feature needs paired C#/Elm inputs and a complete JSON equality test against `morphir-elm make`.
Record reference commands, pinned versions, input hashes, results, and remaining gaps.
Keep reference generation separate from validation. Never adopt converter output to repair parity.
Run locked restore, single-worker build, the full test suite, catalogs, and repository checks.
Update sample, catalogs, mapping, README, AGENTS, and delivery evidence for the requested feature.
Preserve root LICENSE/NOTICE, CLAUDE, historical paired examples, and frozen reference hashes.
D21 requires exact parity coverage for every admitted feature and no pending acceptance gap.

## Commands

```bash
dotnet restore Morphir.CSharp.slnx --packages .packages --locked-mode
dotnet build Morphir.CSharp.slnx --no-restore -m:1
dotnet test Morphir.CSharp.slnx --no-build --no-restore -m:1
python3 eng/check-catalogs.py
python3 eng/check-repository.py
```

VSTest needs a local host socket. Reference generation is a separate reviewed operation.
