# D11 — Exact Elm acceptance

- Date: 2026-11-01.
- Status: passed under the approved collection profile.
- Prerequisite: [D10 passed profile](D10-elm-parity.md).
- Mapping: [approved collection profile](../mappings/D11.md).

Three independent Elm projects cover the 18 admitted collection carriers, explicit elements,
spreads, constructors/factories, read members, nullable-result indexing, and nullable boundaries.
The cumulative sample includes List, Dict, and Set.
The native Elm Array probe is a separate compiler rejection input.
All remaining collection forms keep executable source rejections.
Historical inputs, references, and license bytes remain unchanged.
The existing Classic v3 schema needs no new node.

## Commands

```bash
node eng/generate-exact-references.mjs --write
dotnet restore Morphir.CSharp.slnx --packages .packages --locked-mode
dotnet build Morphir.CSharp.slnx --no-restore -m:1
dotnet test Morphir.CSharp.slnx --no-build --no-restore -m:1
python3 eng/check-catalogs.py --deliveries D1 D2 D3 D4 D5 D6 D7 D8 D9 D10 D11
python3 eng/check-catalogs.py
python3 eng/check-repository.py
```

Reference generation uses only the pinned Elm compiler. Tests never update expected JSON.
Locked restore and build passed; the build has zero warnings/errors.
All 593 tests passed: 556 frontend, 12 integration, 4 model, and 21 codec.
All 80 C# cases matched fresh output from 62 independent Elm projects.
The source registry includes 377 member/form rejections.
The separate Elm Array input failed resolution, as expected.
The project-mode sample CLI matched the complete independent Elm JSON.
The D1–D11 catalog gate has zero pending entries. The global gate has 1,085 later obligations.
Repository checks passed for provenance, history, licenses, plans, links, and whitespace.

Logs: `/tmp/d11-restore.log`, `/tmp/d11-build.log`, `/tmp/d11-references.log`, `/tmp/d11-tests.log`, `/tmp/d11-sample.log`.
D12 can now use this passed prerequisite. Later forms remain outside this approved profile.

The [D12 final regression](D12-elm-parity.md) also covers literal-sized arrays and the computed-dimension rejection.
