> Historical runtime evidence. Current acceptance: [exact Elm parity](elm-parity-state.md).

# D1 — execution evidence

- Status: passed; every D1 acceptance/rejection gate satisfied under the recorded D1 safety boundary and root-byte preservation instruction.
- Date: 2026-10-16.
- Prerequisites: none; bootstrap reconciled against master and D1.
- Scope: D1 only. D2–D21 remain pending. Implementation was validated before commit/push authorization.

## Implementation paths

- `Morphir.CSharp.slnx`, global/common/central pins and per-project locks.
- `src/Morphir.IR.Classic`: generic attribute algebra, typed containers and public specifications.
- `src/Morphir.IR.Json`: explicit v3 codecs, structural comparator, deterministic UTF-8 atomic write.
- `src/Morphir.Frontend.CSharp`: declaration selection, identity table, semantic lowering/proof checks, diagnostics and explicit-file reference acquisition.
- `src/Morphir.Frontend.CSharp.Cli`: repeatable explicit source/namespace arguments, package and atomic output.
- Four `tests` projects, all tagged Delivery=D1; independent Elm sources/codec host and frozen JSON in `fixtures/D1`.
- Cumulative sample; [coverage](../catalogs/language.md), [mapping](../mappings/D1.md), [framework](../catalogs/framework.md), [model/codec](../catalogs/model-codec.md), README and AGENTS.

## Baseline and references

SDK 10.0.100 / MSBuild 18.0.2 / C#14 / reference pack 10.0.0 / runtime 10.0.0 x64; Roslyn 5.0.0. xUnit 2.9.3, runner 3.0.2, test SDK 17.14.1 (SDK VSTest 18.0.1). SDK disables roll-forward; central packages use exact closed version ranges with complete transitive lock hashes. Native semantics are x64 independent of a compilation consumer's host; incompatible x86/ARM32/prefer32 options fail.

Reference tools: Node 20.18.1 / npm 9.2.0 / Morphir Elm 2.100.0 (default ordinary compiler), Elm 0.19.1 host against source revision `71ca7ee385273b0af6f8db66582c26e9b410cbf2`. [Manifest](../../fixtures/D1/expected/provenance.json) records compiler/mode/source/output hashes. Eight complete distributions: ordinary eligibility/prefix/addition; codec private/sample/selection/outside/model. The codec host constructs actual business definitions and qualified references; model case includes dependency specifications/documentation. No frontend-generated expected JSON, private promotion or patched compiled output.

Generation is separate and explicit (`node eng/generate-references.mjs --write`), never part of validation. The final validation uses frozen hashes; no reference edits are permitted during it.

## Recorded decisions and prerequisite corrections

- D1 total Int32 addition is constant-safe or has a proved zero operand. The historical unbounded addition regression is explicitly rejected, with safe specialization parity and boundary behavior. General wrap semantics remain D5; no business-domain bounds are assumed.
- The installed compatible SDK 10.0.100 is the exact D1 pin; the master mentioned historical sample compilation with 10.0.112, not a required implementation pin. No baseline language/framework upgrade is made.
- [License reconciliation](../references/LICENSING.md): root LICENSE/NOTICE bytes are protected and unchanged. The bootstrap root NOTICE was shorter than the pinned upstream file; the original provenance hash referred to the upstream file. D1 preserves root bytes, corrects provenance, and retains exact complete upstream attribution in `docs/references/upstream/morphir-dotnet.NOTICE`, per instruction priority.
- Checked syntax and method return operation regressions were caught during development: discovery failures now stop before emission; lowering starts from the bound return operation (including target-typed new and identity conversions); checked and unchecked syntax kinds are distinguished.
- Sandboxed MSBuild/Node child-process operations and VSTest sockets needed external-sandbox execution permission. Failures were not treated as passes.

## Validation and remaining work

Final command results (all exit 0):

| Command | Result |
| --- | --- |
| `dotnet restore Morphir.CSharp.slnx --packages .packages --locked-mode` | All nine projects restored against exact pins/locks |
| `dotnet clean Morphir.CSharp.slnx -m:1 -v:minimal` | Generated build outputs cleaned |
| `dotnet build Morphir.CSharp.slnx --no-restore -m:1` | All projects/sample built; 0 warnings, 0 errors. Rebuilt successfully after the final bound-conversion fix |
| `dotnet test Morphir.CSharp.slnx --no-build --no-restore -m:1 --filter Delivery=D1 --logger 'trx;LogFilePrefix=verified-D1' --results-directory artifacts/test-results` | **60 passed, 0 failed, 0 skipped**: integration 6; frontend 33; Classic 4; Json 17 |
| README cumulative `dotnet run --project src/Morphir.Frontend.CSharp.Cli --no-build -- ...` invocation | Exit 0; `artifacts/current-supported.json` exactly equals parsed independent `sample.json` |
| `npm ci --prefix eng --ignore-scripts --no-audit --no-fund` | Lockfile installation verified (421 packages); upstream deprecation messages only |
| `node eng/generate-references.mjs --write` | Separate explicit source/codec generation completed before final validation; eight distributions; source/output hashes recorded |
| `python3 eng/check-repository.py` | 24 child plans, original paired code unchanged, Markdown links/fences, frozen reference hashes, root/supplemental license hashes, CLAUDE pointer, tracked/new/staged whitespace passed |
| `git diff --check` / `git diff --cached --check` | Passed; nothing staged/committed at validation |

Final test reports are in ignored `artifacts/test-results/verified-D1_*.trx`; earlier development failures remain separately named and are not counted as final passes. The final validation made no expected-JSON/source/provenance edits. Ordinary/source/codec full structural comparisons include all modules, aliases, docs, access, typed attributes, input order, dependencies and bodies. Behavioral checks independently emit/run C# and evaluate the typed IR at age/addition boundaries, record construction and short-circuit paths. Source/namespace/package/access/partial/nested/overload owner regressions and isolated unsafe/unsupported fixtures passed.

No unresolved D1 work remains. Positive local-alias updates, general M1/Decimal/failure handling, lazy/dynamic behavior, project generators and complete IR coverage remain in their planned deliveries; D1 rejects those source forms. No future delivery is inferred implemented from examples. D2 is the next unpassed delivery. After reviewing the delivery, the user explicitly authorized committing all changes and pushing on 2026-10-16.
