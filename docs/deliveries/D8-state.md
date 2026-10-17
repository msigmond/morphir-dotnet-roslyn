# D8 — execution evidence

- Status: passed. Prerequisites D5 and D7 passed; D7 be0b46c5e5115f44e78289f1ae0e890d4f287d8f and D6 c22ec6f are pushed and verified. D9/D10 implementation remains pending until D8 is published.
- Fixed baseline retained: SDK10.0.100, C#14, reference pack10.0.0, Roslyn5.0.0, x64. Master/child/prerequisite evidence read; all24 embedded paired examples unchanged.

## Implementation and independent evidence

- [D8 mapping](../mappings/D8.md) records exact source owners, numeric enum domains/constants/aliases/flags/unnamed values, all eight underlying types, conversions/defaults/nullable lifting, and no closed-named-case restriction.
- Scoped value/reference tuple types/values/patterns, labels/ItemN/Rest, constructors/Create factories, nested/long forms, M1/default elements, element conversions, deconstruction, local/by-value value copying/updates and source element equality are implemented. Invalid rest construction, unknown operators, checked forms and unsafe dereferences reject with source locations. Unreachable tuple safety is validated conservatively. General user data/dispatch/generics/collection/API closure remains gated.
- Classic now has separate alias/custom definitions and alias/custom/opaque specifications. Public projections hide private constructors. The two production compatibility factories were mechanically re-emitted from unchanged independently compiled Elm IR for the type-container migration. Only their C# types and generated-byte hashes changed; production IR/source and all D1–D7 golden references are unchanged.
- Separate reviewed operation: `node eng/generate-d8-references.mjs --write`. New original ordinary Elm enum output and complete original Elm codec tuple/flags/model/cumulative distributions were generated with pinned source71ca7ee385273b0af6f8db66582c26e9b410cbf2 and Morphir Elm2.100.0. Exact modes/source/provenance hashes are frozen in fixtures/D8/expected/provenance.json. Validation never invokes generation or writes expected JSON.
- First parity run found redundant tuple reconstruction for label-only changes. The frontend now preserves those values and maps only actual element conversions. Expected data was unchanged after validation began. All corrected complete source/flags/model parity and native behavior checks passed.
- CurrentSupportedExample/EnumsTuples.cs, reference pointer, language/forms/operations/framework/model catalogs, mapping, README and AGENTS are maintained. Catalog check:2,075 entries with1,238 explicit later/final obligations; no final full-framework/API claim.

## Commands and results

- `dotnet restore Morphir.CSharp.slnx --packages .packages --locked-mode`: passed; artifacts/d8-restore.log.
- `dotnet build Morphir.CSharp.slnx --no-restore -m:1`: passed, zero warnings/errors; artifacts/d8-final-build.log.
- Focused `Delivery=D8` solution run:21 passed, zero failures/skips (20 integration,1 Json), including all eight enum boundary domains and complete frozen parity. artifacts/d8-corrected-gates.log.
- Source CLI: all three complete Enums/Tuples/Flags distributions matched frozen references. Unsafe tuple access returned1/MCS2501, preserved an existing artifact byte-for-byte and created no fresh artifact.
- `python3 eng/check-repository.py`, `python3 eng/check-catalogs.py`, `git diff --check`: passed. All prior references, protected LICENSE/NOTICE, CLAUDE pointer and generated provenance verified.
- Complete `Delivery=D1|Delivery=D2|Delivery=D3|Delivery=D4|Delivery=D5|Delivery=D6|Delivery=D7|Delivery=D8` run passed162 tests, zero failures/skips (111 integration,28 frontend,4 Classic,19 Json). Logs:artifacts/d8-final-gates.log; reports:artifacts/test-results/final-D8_*.trx. Actual cumulative project/generator/source-reference capture, complete sample parity and CLI project mode all passed. Frozen references were unchanged throughout validation.

## Continuation

- Previous ordinary capacity reset was observed before resuming. No reset credits or billing actions were used. Existing continue-morphir-d2-d10 hourly follow-up remains active until D10 passes/is pushed.
- All implementation and required gates passed. Publication remains: staged repository checks, commit all D8 changes, push origin/main using authenticated gh and verify the remote SHA, then begin D9. No automatic commit of incomplete/failed work.
