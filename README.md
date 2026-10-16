# Morphir .NET Roslyn

C# data and pure business logic → Morphir Classic IR, format version 3.

- status: planning/bootstrap only; D1 implementation pending
- target: .NET 10, C# 14, explicit 64-bit native integers
- execution: CLI first; MSBuild later
- reference: Morphir Elm 2.100.0; structural JSON equality

## Plan

- [Master plan](Morphir_CSharp_Frontend_delivery_plan.md): decisions, layout, gates, delivery index
- [D1](Morphir_CSharp_Frontend_deliveries/D01-cli-record.md): first executable feature
- [AGENTS.md](AGENTS.md): repository workflow
- [Historical documents](docs/history/): context only; master takes precedence
- [Archived addition regression](docs/references/prototype-addition/PROVENANCE.md): migration input only

## Current contents

- Master + 24 delivery plans; paired C#/Elm examples
- Bootstrap README and agent instructions
- Mirrored upstream license and attribution
- Historical plans and addition regression inputs

- implementation: no solution/build scripts/test suite yet
- build/test/CLI commands: D1, after verification
- cumulative `CurrentSupportedExample` project: D1 onward

## Initial review

```sh
git status --short
git diff --cached --stat
git diff --cached
```

Commit/push: after user review and authorization.

## License

[Apache License 2.0](LICENSE); [NOTICE](NOTICE); [source and hashes](docs/references/LICENSING.md).
