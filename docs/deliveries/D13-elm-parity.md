# D13 — Exact Elm acceptance

- Date: 2026-11-01.
- Status: passed under the approved profile.
- Prerequisite: [D12 passed profile](D12-elm-parity.md).
- Mapping: [approved anonymous-function profile](../mappings/D13.md).

The prerequisite suite passed all 632 tests before implementation.
Paired inputs cover lambdas, anonymous methods, lexical captures, generic callbacks,
nullable results, blocks, invocation, explicit construction, and leading-lambda inputs.
Excluded forms have executable C# rejection cases.
The sample includes a captured lambda. Existing Classic Lambda and function nodes are used.
The schema and codec need no extension.

## Commands

```bash
node eng/generate-exact-references.mjs --write
dotnet restore Morphir.CSharp.slnx --packages .packages --locked-mode
dotnet build Morphir.CSharp.slnx --no-restore -m:1
dotnet test Morphir.CSharp.slnx --no-build --no-restore -m:1
python3 eng/check-catalogs.py --deliveries D1 D2 D3 D4 D5 D6 D7 D8 D9 D10 D11 D12 D13
python3 eng/check-catalogs.py
python3 eng/check-repository.py
```

Locked restore passed. The build passed with zero warnings/errors.
All 656 tests passed: 618 frontend, 13 integration, 4 model, and 21 codec.
All 88 C# cases matched fresh output from 70 independent Elm projects.
The registry has 431 executable member/form rejections.
The D1–D13 catalog gate has zero pending entries. There are 1,057 later obligations.
All 46 guide inputs bind as C#14 and fail conversion as documented.
Repository checks passed for all 24 plans, frozen examples/references, licenses, links, hashes, and whitespace.
Reference generation used only the pinned Elm compiler. Tests did not update expected JSON.

Logs: `/tmp/d13-prerequisite-tests.log`, `/tmp/d13-restore.log`, `/tmp/d13-build.log`,
`/tmp/d13-references.log`, `/tmp/d13-final-tests.log`. Guide checks: `/tmp/d13-guide-check`.
See the [D11 follow-up result](D11-follow-up-elm-parity.md) for collection calls and the project-mode sample.
D14–D21 remain planned. Unapproved forms retain the explicit failures in the mapping guide.
