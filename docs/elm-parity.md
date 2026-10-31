# Morphir Elm parity contract

User instruction: 2026-10-31. This contract controls all remaining work.
Deliver standard Morphir IR that matches `morphir-elm make` output.
Source v1 was not requested. It is not an accepted feature or an alternative delivery target.
The approved source-structure rules still prohibit hidden CLR simulation and runtime safety proofs.

## Feature gate

Every feature needs an automated test with equivalent C# and Elm inputs.

1. Define the paired inputs before implementation. Use the same package, names, visibility, and types.
2. Run the pinned `morphir-elm make` command on the Elm project. Record the command and compiler version.
3. Run the C# transpiler on the C# input.
4. Compare the complete parsed JSON for equality. Ignore only whitespace and JSON object-key order.
5. Retain all array order, names, types, access flags, definitions, literals, and operations.
6. Run the feature test and regression checks before a completion claim.

No package rewriting, alpha-renaming, type substitution, sorting, filtering, or field removal is permitted in this gate.
Expected JSON must come from `morphir-elm make`. Do not use C# output or a hand-authored codec distribution as a substitute.
Frozen compiler output can be used by regression tests. Keep paired inputs, version, command, and hashes so it can be reproduced.
Reference generation is separate from validation. Validation must not repair its own expected output.
Source assertions and rejection tests supplement exact parity; they do not replace it.
An explained difference does not count as a passing exact-parity test.

For example, explicit local `: Int` annotations in the paired Elm fixture avoid retained inference variables.
Keep these annotations in the fixture input. Do not add them or replace type variables during comparison.

## Scope

Implement only the requested feature and the changes needed for its exact-parity test.
Do not add output formats, syntax envelopes, helper libraries, or runtime emulation.
If the Elm counterpart or mapping is unclear, ask the user before implementation.
Unmapped forms retain an explicit source-located failure and atomic output.
Do not implement a requested feature as a custom Source node to avoid the parity gate.

## Existing gaps

D1–D9 passed earlier Source-mode checks. That result does not establish acceptance under this contract.
Their feature coverage needs exact Elm parity revalidation before it can be called complete.
The existing business comparison permits package, local-name, and local-type normalization.
It is supplemental evidence and does not satisfy the exact-parity gate.
Historical codec and runtime references remain frozen; their existence alone does not establish frontend parity.
The existing public Source v1 mode is unrequested code. Its removal remains corrective cleanup.
Do not extend it or count it as delivered capability.
D10–D21 remain planned and must use this gate for every feature.
