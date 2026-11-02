# D11 follow-up — Exact Elm acceptance

- Date: 2026-11-01.
- Status: passed under the approved profile.
- Prerequisite: [D13](D13-elm-parity.md).
- Mapping: [approved collection calls](../mappings/D11.md#collection-operation-follow-up).

Paired inputs cover filter, map, List copies, callback values, method references,
generic and nullable results, named arguments, chained calls, and seeded folds.
The approved fold adapter reverses callback argument order without inlining its body.
Excluded overloads have executable C# rejection cases.
The sample includes filter, map, and fold. The schema and codec need no extension.

The final [commands and regression results](D13-elm-parity.md#commands) cover both deliveries.
All 656 tests passed, including complete equality for 88 C# cases and fresh output from 70 Elm projects.
The D1–D13 catalog gate has zero pending entries. The global gate has 1,057 later obligations.
The fold cases include parameter, method-reference, and unused-local name collisions.
The adapter uses a numeric suffix where required. It does not alter the written callback.

## Project-mode sample

```bash
dotnet run --project src/Morphir.Frontend.CSharp.Cli --no-build -- --package Example --namespace Example.CurrentSupported --project samples/CurrentSupportedExample/CurrentSupportedExample.csproj --framework net10.0 --output artifacts/d13-current-sample.json
```

The command passed. Its complete parsed JSON equals the independent current-sample Elm output.
Log: `/tmp/d13-sample.log`. No packages, names, types, fields, or array order were changed in comparison.
The earlier [D11 evidence](D11-elm-parity.md) remains the record for the original collection profile.
Indexed callbacks, unseeded folds, result selectors, query syntax, Set/Dict enumeration, and unlisted calls remain rejected.
