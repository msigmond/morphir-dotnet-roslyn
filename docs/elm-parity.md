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

## Current profile

D1–D9 implement the [approved mappings](reviews/d1-d9-elm-mapping-decisions.md) with exact tests and explicit rejections.
D10 adds the user-approved [generic profile](mappings/D10.md).
See [D10 validation](deliveries/D10-elm-parity.md), [earlier validation](deliveries/elm-parity-state.md), and [limits](classic-output.md).
Source v1 is removed. It cannot return as an alternative output.
Mutation, identity/cycles, side-effecting construction, and other unapproved forms remain rejected.
D11–D21 remain planned and must use the same feature gate.

## Existing gaps

The remaining gaps belong to later deliveries or explicit rejection boundaries.
Earlier Source-mode passes, raw syntax capture, normalized comparisons, and explained differences do not pass this gate.
