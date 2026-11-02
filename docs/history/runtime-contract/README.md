# Former runtime contract

This archive preserves the D1–D9 implementation, frontend tests, mappings, and plans before 2026-10-24.
The [approved audit](../../reviews/source-structure-audit.md) replaces that runtime-equivalence contract.
The current [exact Elm contract](../../elm-parity.md) and [validation evidence](../../deliveries/elm-parity-state.md) control acceptance.

`frontend/` preserves removed execution/proof lowering code.
`tests/` preserves superseded CLR behavior, helper parity, and safety-rejection tests outside active test projects.
`plans/` preserves the original master and 24 child plans as text snapshots.
`mappings/` preserves the original D1–D9 mappings as text snapshots.
Relative links inside text snapshots refer to their original repository context.

Frozen fixtures, compatibility source/factory hashes, licensing bytes, and Classic codec references retain their bytes.
The D9 data input is now [a regression fixture](../../../fixtures/D9/csharp/LegacyData.cs). Its provenance path changes; its SHA-256 stays unchanged.
Archived paths inside snapshots record their original context.
They are still checked. They are not regenerated as source v1 output.
The current test suite replaces runtime obligations with source-structure, typed mapping, binding, and adapter checks.
No archived test result proves source v1 support.
