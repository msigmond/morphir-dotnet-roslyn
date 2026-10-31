# D1–D9 exact Elm acceptance

- Date: 2026-10-31.
- Status: passed under the approved Elm profile.
- Contract: [exact Elm parity](../elm-parity.md).
- Decisions: all six [mappings](../reviews/d1-d9-elm-mapping-decisions.md) are approved.
- Output: standard Classic v3 only. Source v1 is removed.

## Scope and evidence

The [registry](../../fixtures/elm-parity/cases.json) has 73 C# cases and 55 independent Elm projects.
Each positive case compares the complete JSON. Integration tests run pinned `morphir-elm make` again.
There is no package, name, type, array, or operation normalization.

| Delivery | Exact-tested scope |
| --- | --- |
| D1 | Source/CLI, owners, selection, access, names, literals, records, overload suffixes |
| D2 | Project/reference/generated inputs, additional files, options, atomic failures |
| D3 | Locals, scope, returned branches, early returns, constant/unreachable branches |
| D4 | Maybe/default operations, nullable references, delegates, inferred locals, presence tests |
| D5 | Scalar operations/conversions, written literals, numeric member grid |
| D6 | Decimal construction/members, direct nullable-result division, member grid |
| D7 | Parameter order/defaults/params, references/delegates, lexical binding graphs, recursion, configured metadata |
| D8 | Enum aliases/constants, value/reference tuples, projections, deconstruction |
| D9 | Data aliases, primary/explicit constructor functions, getters/operators, record equality/updates |

The grids contain 320 admitted member inputs. Division has separate nullable-context cases.
There are 243 executable member/form rejections, plus the audit and compiler/selection tests.
Two native unsigned operator forms fail C#14 binding; those compiler errors remain unchanged.
The audit now requires either its recorded positive result or its recorded rejection.
Raw syntax capture cannot pass either gate.

The sample includes the approved forms and has its own independent Elm project.
Historical reference bytes, SDK/reference/Roslyn pins, and LICENSE/NOTICE bytes remain unchanged.
Historical runtime/Source state files do not control current acceptance.

## Commands

From the repository root:

```bash
node eng/generate-exact-references.mjs --write
dotnet restore Morphir.CSharp.slnx --packages .packages --locked-mode
dotnet build Morphir.CSharp.slnx --no-restore -m:1
dotnet test Morphir.CSharp.slnx --no-build --no-restore -m:1
python3 eng/check-catalogs.py --deliveries D1 D2 D3 D4 D5 D6 D7 D8 D9
python3 eng/check-catalogs.py
python3 eng/check-repository.py
```

Reference generation is separate from validation. Expected JSON comes only from Elm make.
The member audit records source inputs and diagnostics; it never supplies reference IR.
Project tests disable MSBuild node reuse and bound output reads to prevent orphan pipe waits.

## Results

Locked restore passed. Build passed with zero warnings/errors.
The final complete run passed 450 tests: 415 frontend, 10 integration, 4 model, and 21 codec.
All 73 C# cases matched fresh output from the 55 Elm projects, including nullable Decimal.Divide.
The documented project-mode sample CLI command also passed and matched the complete independent Elm output.
The D1–D9 catalog gate has zero pending entries. The global gate retains 1,190 later-delivery obligations.
Repository checks passed: all plans, paired historical code, links, hashes, licenses, and whitespace.

Logs: `/tmp/d1d9-closed-restore.log`, `/tmp/d1d9-final-release-build.log`,
`/tmp/d1d9-final-release-references.log`, `/tmp/d1d9-final-tests.log`, `/tmp/d1d9-final-sample.log`.

## Boundary

Approved mappings and required rejections complete this profile; they do not claim all C# forms.
Mutation, identity/cycles, recursive aliases, incomplete construction, unapproved numeric APIs,
nullable extraction/flattening, and later forms remain rejected. See [limits](../classic-output.md).
D10–D21 remain planned. Passed deliveries follow the authorized commit/push workflow.
