# Exact Elm parity correction

- status: passed for the supported subset on 2026-10-31
- contract: [exact Elm parity](../elm-parity.md)
- scope: remove unrequested Source v1 output; enforce complete JSON equality for the supported Classic subset
- policy commit: `503aa14`, pushed before code changes
- user decisions: reject unmapped forms until a specific mapping is approved; map decimal literals to Elm Decimal constructor calls

## Changes

Removed the public format switch, Source settings, source envelope, syntax metadata, and Source model/codec types.
C# operation staging is internal and cannot be emitted by the standard codec.
Removed the normalized comparison helper and replaced Source-mode success tests with exact parity or explicit rejection checks.
Added 49 C# cases, 31 independent Elm projects, version/command/hash provenance, and fresh compiler integration checks.
The current sample now compiles as the supported Classic subset and has an independent complete Elm reference.
Historical examples and frozen references remain unchanged.

Fixed Half arithmetic mapping and floating literal parsing without CLR Single quantization.
Removed obsolete intrinsic type markers that wrongly reserved the user package name Morphir.CSharp.
Decimal literals map to `Decimal.fromFloat`; Decimal unary negation maps to `Decimal.negate`.
Isolated UTF-16 surrogates, unapproved constructor/accessor/operator forms, enum declarations, and overloaded names fail explicitly.
Every supported catalog entry names its exact-parity cases. Unsupported earlier Source claims are pending.
The catalog checker rejects supported entries without valid case IDs and verifies dependency inputs against their compiled Elm provider.

## Validation

From the repository root:

```bash
node eng/generate-exact-references.mjs --write
dotnet restore Morphir.CSharp.slnx --packages .packages --locked-mode
dotnet build Morphir.CSharp.slnx --no-restore -m:1
dotnet test Morphir.CSharp.slnx --no-build --no-restore -m:1 --logger 'trx;LogFilePrefix=exact-accepted'
python3 eng/check-catalogs.py
python3 eng/check-repository.py
```

Reference generation is explicit and separate from tests. Expected JSON comes only from `morphir-elm make`.
Tests copy Elm inputs to artifacts, run the pinned compiler, compare fresh output to frozen output,
and compare each C# result to that complete fresh distribution. They do not normalize semantic content.
The dependency provider is also compiled with `make`; the CLI uses its supported data-URL dependency input.
The package versions, baseline, root licenses, and frozen historical outputs are unchanged.

Results:

- Locked restore passed. Build passed with zero warnings and errors.
- All 181 tests passed: 148 frontend, 8 integration, 4 model, 21 JSON codec.
- Fresh `morphir-elm make` output matched all 49 C# cases across 31 Elm projects.
- Catalog checks passed: 2,077 entries; 1,680 future acceptance obligations remain.
- Repository checks passed: all plans, paired examples, links, hashes, licenses, and whitespace.
- The documented current-sample CLI command passed and matched the complete independent Elm JSON.
- The real SomeBusinessLogic project also matched the complete typed Elm business reference and its existing generated IR. External project/output files were not changed.
- A final provenance refresh changed no bytes in any of the 31 expected Elm outputs.

After the final diagnostic-text correction, build passed again and all 9 affected compiler/selection/rejection tests passed.

Logs: `/tmp/exact-accepted-restore.log`, `/tmp/exact-accepted-build.log`, `/tmp/exact-accepted-tests.log`.
TRX reports use prefix `exact-accepted` in each test project's `TestResults` folder.

## Remaining scope

This correction completes neither the full D1–D9 inventory nor D10–D21.
It establishes exact parity for the supported cases and rejects forms without an approved standard mapping.
Catalog pending entries remain visible; earlier Source-mode success is not accepted feature evidence.
