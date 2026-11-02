# Single-file business demo

- Request: one structured example for the approved business feature families.
- Business: a fictional festival robot-rental company.
- Status: passed, including removal of superseded samples.
- Date: 2026-11-01.

The active example is [BusinessLogic.cs](../../samples/FestivalRobotRental/BusinessLogic.cs).
All business data, inputs, and rules are in that file. The project has one explicit Compile item and no generator.
The [guide](../../samples/FestivalRobotRental/README.md) maps each feature family to named functions.
All other sample directories are removed. Only inputs needed by active regressions or frozen provenance remain in `fixtures`.

The [independent Elm project](../../fixtures/elm-parity/festival-robot-rental/elm/morphir.json)
uses only approved mappings. It supplies complete expected JSON for the registered `festival-robot-rental` case.
Tests cover the demo result and complete JSON equality in source, project, and CLI modes.
No frontend feature, mapping, format, or toolchain version is added.

## Commands and results

```bash
node eng/generate-exact-references.mjs --write
dotnet restore Morphir.CSharp.slnx --packages .packages --locked-mode
dotnet build Morphir.CSharp.slnx --no-restore -m:1
dotnet test Morphir.CSharp.slnx --no-build --no-restore -m:1
python3 eng/check-catalogs.py --deliveries D1 D2 D3 D4 D5 D6 D7 D8 D9 D10 D11 D12 D13
python3 eng/check-catalogs.py
python3 eng/check-repository.py
```

Locked restore and build passed with zero warnings/errors.
All 659 tests passed: 620 frontend, 14 integration, 4 model, and 21 codec.
All 89 C# cases match complete output from 71 independent Elm projects.
The new project, source, and both CLI modes match the complete demo reference.
The business test checks acceptance flags, quotes, robots, cartons, charge, capacity use, and loyalty credit.

The D1–D13 catalog gate has zero pending entries. The global gate has 1,057 later obligations.
Repository checks preserve frozen source/reference bytes, all 24 plans, licenses, links, and whitespace.
The earlier sample case remains a regression fixture. The generator/project tests use `fixtures/project-system/ThresholdGenerator`.

The paired Elm boolean expressions use explicit parentheses to match C# operator grouping.
Expected JSON comes only from the pinned Elm compiler. No output normalization is used.

Logs: `/tmp/business-demo-restore.log`, `/tmp/business-demo-build.log`,
`/tmp/business-demo-references.log`, `/tmp/business-demo-focused-tests.log`, and `/tmp/business-demo-final-tests.log`.

## Removal of superseded samples

The user requested removal of all other samples and commit/push.
Only `samples/FestivalRobotRental` remains. The repository check enforces one sample directory and one business source.
Unused example files and the unused shared project are deleted.
The old ClassicLoan input moves to the existing exact-parity fixture.
The D9 data input moves to `fixtures/D9/csharp/LegacyData.cs`; its frozen SHA-256 stays unchanged.
The earlier structural-audit input moves to `fixtures/source-structure/Input.cs`.
Generator acquisition tests now use the existing project-generated fixture.
Frozen output JSON, historical snapshot bytes, and license bytes stay unchanged.

Cleanup validation passed: locked restore, build with zero warnings/errors, all 659 tests,
D1–D13 catalog gate, and repository checks. All 89 C# cases still match 71 independent Elm projects.
The existing expected IR JSON is unchanged. The D9 provenance changes only its relocated source path.
Logs: `/tmp/sample-cleanup-restore.log`, `/tmp/sample-cleanup-build.log`,
`/tmp/sample-cleanup-references.log`, and `/tmp/sample-cleanup-tests.log`.
