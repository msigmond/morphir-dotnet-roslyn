> Historical result under earlier checks. It does not establish acceptance under the current [exact Elm parity contract](../elm-parity.md). D1–D9 need revalidation.

> Historical validation for source mode. The default changed to Classic v3 on 2026-10-31; see [current correction](classic-output-state.md).

# Source structure migration evidence

- status: passed, 2026-10-24
- authorization: approved audit and cross-delivery implementation request
- scope: all 62 audit cases; D1–D9 revalidation; master and all 24 child plans rewritten
- next delivery: D10; D10–D21 remain planned
- contract: [Morphir.CSharp.Source v1](../source-structure.md)
- previous runtime evidence: historical D1–D9 state files and [frozen archive](../history/runtime-contract/README.md)

## Result

The frontend reads source syntax and resolved symbols without runtime flow, range, purity, ownership, or termination proofs.
Nonnullable strings and data inputs stay nonnullable. Declared results control output types.
Integer and Decimal products contain two field reads and one direct multiplication.
Written statements, calls, casts, constructors, accessors, null operations, checks, and branches remain source structure.
No numeric compatibility library, hidden initialization, synthesized equality, heap simulation, or inlined callee is emitted.

SourceValue retains forms absent from Classic. Full selected declaration syntax retains spelling, types, bindings, and locations.
The serializer marks extended output as Morphir.CSharp.Source v1. It rejects source operations in an unmarked Classic envelope.
The independent plain Classic v3 codec remains active and its references are unchanged.
This is a source-analysis contract; it does not reproduce CLR execution.

The former runtime implementation/tests and old plans/mappings are archived with a byte-hash manifest.
Frozen source/Elm references, compatibility factories, toolchain pins, and license bytes remain unchanged.
The factories are excluded from the frontend compile inputs.

All 24 plans, current mappings, catalogs, README, AGENTS, schema, and cumulative sample are updated.
Historical paired C#/Elm examples are unchanged and clearly marked as former mappings.
Direct recursion and local captures now retain source references. Their extended future closure remains planned.

## Validation

| Command | Result |
| --- | --- |
| `dotnet restore Morphir.CSharp.slnx --packages .packages --locked-mode` | Passed; pinned packages retained |
| `dotnet build Morphir.CSharp.slnx --no-restore -m:1` | Passed; zero warnings and errors |
| `dotnet test Morphir.CSharp.slnx --no-build --no-restore -m:1 --logger 'trx;LogFilePrefix=source-complete'` | Passed: 145; failed: 0; skipped: 0 |
| `python3 eng/check-catalogs.py` | Passed: 2,075 entries; 1,202 later closure obligations retained |
| `python3 eng/check-repository.py` | Passed: 24 plans, paired examples, links/fences, frozen references/archive, licenses, pointer, whitespace |
| `git diff --check` and staged whitespace check | Passed |

Test totals: frontend 91; integration 28; Classic model 4; JSON/codec 22.
VSTest and project loading ran with the required local host sockets enabled.
Logs and TRX files are local under artifacts/source-structure and test-project TestResults directories.

## Evidence coverage

- AuditMatrixTests: all 62 audit IDs; complete selected token structure; no runtime expansion nodes/helpers.
- SourceStructureTests: exact Loan/integer products, declared nullability, disabled nullable analysis, inferred flow, branches, arguments, calls, data bodies, and source-located rejection boundaries.
- StructureIntegrationTests: all self-contained D1–D9 fixtures; original external fixture bound to metadata; actual project/reference/generator inputs; cumulative source parity; CLI parity and failed-output preservation.
- FrameworkInventoryTests: every pinned public numeric and Decimal declaration retained exactly.
- SourceCodecTests: explicit extension format, literal tags, depth, and protection against mislabelled Classic output.
- Existing Classic model/codec, specification/access, provenance, licensing, and strict JSON comparison checks remain active.

The [sample contract](../../fixtures/source-structure/contract.json) was authored from the approved requirements.
No source v1 golden was generated from converter output. No old expected JSON was refreshed.

## Remaining work

D10–D21 implement their listed source mappings and closure checks. They are not completion claims of this migration.
Standard Classic v3 consumers require a source v1 reader for extended output.
Source metadata is authoritative where the Classic projection omits source type/form distinctions.
Whitespace and comment round-tripping is outside this structural contract.
