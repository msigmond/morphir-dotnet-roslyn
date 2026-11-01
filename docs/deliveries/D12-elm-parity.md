# D12 — Exact Elm acceptance

- Date: 2026-11-01.
- Status: passed under the approved direct case profile.
- Prerequisite: [D11 passed profile](D11-elm-parity.md).
- Mapping: [approved direct case profile](../mappings/D12.md).

Four independent Elm projects cover literal/enum/nullable patterns, tuples, list prefixes/tails,
case bindings, case order, returned switch statements, nested switches, and result contexts.
The cumulative sample includes returned switches and list/nullable patterns.
The separate negative-pattern Elm input records the pinned parser failure.
Excluded forms have bound executable C# rejection cases.
The standard LiteralPattern node is added; no new output format is introduced.
Historical sources, references, and license bytes remain frozen.

## Commands

```bash
node eng/generate-exact-references.mjs --write
dotnet restore Morphir.CSharp.slnx --packages .packages --locked-mode
dotnet build Morphir.CSharp.slnx --no-restore -m:1
dotnet test Morphir.CSharp.slnx --no-build --no-restore -m:1
python3 eng/check-catalogs.py --deliveries D1 D2 D3 D4 D5 D6 D7 D8 D9 D10 D11 D12
python3 eng/check-catalogs.py
python3 eng/check-repository.py
```

Reference generation uses only the pinned Elm compiler. Tests never update expected JSON.
Locked restore and build passed; the build has zero warnings/errors.
All 632 tests passed: 594 frontend, 13 integration, 4 model, and 21 codec.
All 84 C# cases matched fresh output from 66 independent Elm projects.
The registry contains 411 executable member/form rejections.
The final project-mode sample CLI matched the complete independent Elm JSON.
All 45 guide examples passed C#14 binding and failed conversion as documented.
The separate negative-pattern Elm input failed parsing, as expected.

Final review also added a paired literal-size array initializer and rejected computed dimensions.
This prevents loss of written arithmetic during List conversion.
The D1–D12 catalog gate has zero pending entries. The global gate has 1,063 later obligations.
Repository checks passed for plans, provenance, frozen history, licenses, links, and whitespace.

Logs: `/tmp/d12-restore.log`, `/tmp/d12-build.log`, `/tmp/d12-references.log`,
`/tmp/d12-final-tests.log`, `/tmp/d12-sample.log`. Guide checks: `/tmp/d12-guide-check`.
D13–D21 remain planned. Unapproved forms retain the explicit failures in the mapping guide.
