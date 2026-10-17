# Licensing provenance and D1 reconciliation

- Source: [finos/morphir-dotnet](https://github.com/finos/morphir-dotnet), commit `7c0c06d1fe0c9d1fcb3b793f5342a4dac58518ad`.
- Bootstrap copy date and D1 verification: 2026-10-16.
- Root LICENSE and NOTICE remain byte-for-byte unchanged from bootstrap commit `9d84de7`, as required by the repository instructions.
- Verification: `sha256sum LICENSE NOTICE docs/references/upstream/morphir-dotnet.NOTICE`.

| File | SHA-256 | Provenance |
| --- | --- | --- |
| Root `LICENSE` | `b40930bbcf80744c86c46a12bc9da056641d722716c378f5659b9e555ef833e1` | Exact [upstream LICENSE](https://raw.githubusercontent.com/finos/morphir-dotnet/7c0c06d1fe0c9d1fcb3b793f5342a4dac58518ad/LICENSE) bytes |
| Root `NOTICE` | `24564967220aaade6950d5442cdab8d48033abde6c7906285a03a8b98a45542a` | Preserved bootstrap file; includes FINOS attribution |
| [Complete upstream NOTICE](upstream/morphir-dotnet.NOTICE) | `5021f8b9a8acf4e3040b2320199ead46981f9719b5a99d7947f5e37b8fb530f9` | Exact [upstream NOTICE](https://raw.githubusercontent.com/finos/morphir-dotnet/7c0c06d1fe0c9d1fcb3b793f5342a4dac58518ad/NOTICE) bytes, including Morgan Stanley copyright/attribution |

The bootstrap provenance originally assigned the upstream NOTICE hash to the shorter root NOTICE and incorrectly described it as an exact copy. D1 fetched both pinned upstream files and verified this discrepancy: the root NOTICE omits the upstream Morgan Stanley copyright and development-attribution lines. To obey the higher-priority instruction to preserve root bytes while retaining complete upstream attribution, D1 adds the exact supplemental upstream NOTICE above and corrects this record. No copyright text has been removed or modified by D1. Future release packaging must include the supplemental attribution alongside root LICENSE/NOTICE; root restoration would require an explicit change to the preservation instruction.

## Reference toolchain

Morphir Elm reference compiler: `2.100.0`, source [commit 71ca7ee385273b0af6f8db66582c26e9b410cbf2](https://github.com/finos/morphir-elm/tree/71ca7ee385273b0af6f8db66582c26e9b410cbf2), Apache-2.0. Its unmodified source is fetched into ignored `artifacts/morphir-elm` for host compilation; upstream headers/licenses remain intact. No vendored upstream implementation is included in src. npm packages preserve their own license files in the tool installation; exact packages/integrities are recorded in `eng/package-lock.json`. Generated independent fixtures and source/output hashes are tracked in [D1 provenance](../../fixtures/D1/expected/provenance.json).
