# Classic output correction

- status: passed on 2026-10-31
- authorization: Classic v3 default confirmed on 2026-10-31
- cause: the prior default used custom Source v1 tags rather than standard Morphir IR
- contract: [Classic output](../classic-output.md)

The correction adds explicit Classic mappings for blocks, locals, returns, direct calls, and positional record constructors.
It removes empty modules and separate primary-constructor functions from Classic output.
It fixes object-creation types and keeps global method references distinct from same-named local bindings.
The original C# package, visibility, declared types, and local names remain unchanged.
Source mode is explicit. No CLR proof rules or compatibility libraries are restored.

The supplied corrected Elm output and both source sets are frozen in the independent business fixture.
Comparison retains the full distribution, with documented package placement, alpha-renaming, and local type-variable resolution.
The original command regenerated `/home/marton/VSCodeProjects/SomeBusinessLogic/IR/morphir-ir.json` as Classic v3.
The generated distribution has two modules. It equals the tested CLI fixture output in full.
The package remains `Sigmond.Buslog`, as requested by that command.
The Elm file uses package `Sigmond`. This changes the encoded package/module boundary, not the namespace.
The current Elm reference has `dailyFee : Loan -> Int`; its JSON hash matches the supplied file.

## Checks

Run from the repository root with SDK 10.0.100:

```bash
dotnet restore Morphir.CSharp.slnx --packages .packages --locked-mode
dotnet build Morphir.CSharp.slnx --no-restore -m:1
dotnet test Morphir.CSharp.slnx --no-build --no-restore -m:1 --logger 'trx;LogFilePrefix=classic-verified'
python3 eng/check-catalogs.py
python3 eng/check-repository.py
```

Results: restore passed; build passed with zero warnings and errors; all 168 tests passed
(113 frontend, 29 integration, 4 Classic model, 22 JSON codec).
Catalog checks passed: 2,075 entries; 1,202 future obligations remain.
Repository checks passed: 24 plans, paired code, links, fences, frozen hashes, licenses, and whitespace.
Build/test logs: `/tmp/classic-verified-build.log`, `/tmp/classic-verified-tests.log`.
TRX reports use prefix `classic-verified` in each test project's `TestResults` folder.
The checks cover complete Elm parity for both package boundaries, concrete return and constructor types,
local/global binding scope, simple arithmetic, explicit Source mode, and atomic Classic rejection.
Negative parity checks retain operator and visibility differences.

## Project validation

The exact supplied command passed with exit code 0:

```bash
dotnet run --project src/Morphir.Frontend.CSharp.Cli --no-build -- --package Sigmond.Buslog --namespace Sigmond.Buslog.Model --namespace Sigmond.Buslog.Algo --project /home/marton/VSCodeProjects/SomeBusinessLogic/SomeBusinessLogic.csproj --framework net10.0 --configuration Debug --output /home/marton/VSCodeProjects/SomeBusinessLogic/IR/morphir-ir.json
```

The prior file was backed up to ignored `artifacts/classic-correction/previous-business-ir.json`.
Only the output IR was replaced. The external C# and Elm source/configuration files were not changed.

## Remaining work

Classic mappings outside the documented subset remain explicit gaps.
The broader D1–D9 Source-mode coverage remains available. D10–D21 remain planned.
This correction does not claim complete C# or complete Classic model support.
No historical reference, toolchain pin, LICENSE, or NOTICE was changed.
