# D1–D9 mapping decisions

Status: all six mappings approved by the user on 2026-10-31.
The [exact Elm parity contract](../elm-parity.md) controls acceptance.
Each approved mapping must have paired inputs and complete compiler JSON equality.

| Area | Approved mapping | Effect |
| --- | --- | --- |
| D1/D7 overload names | Use signature suffixes, for example `f0NoneInt` and `f0NoneLong`. | Elm has no overloaded value names. Both paired inputs must use these names. |
| D7 named/default/params calls | Put arguments in parameter order. Supply declared omitted defaults. Map expanded params to an Elm List. | Replaces the former rule that kept only written arguments and inserted no defaults or params allocation. |
| D4 nullable operations | Null → Nothing; non-null nullable result → Just; `??` → Maybe.withDefault; `?.` → Maybe.map; lifted arithmetic → Maybe.map2; presence tests → comparison with Nothing. | Adds standard Maybe constructors/calls required by the Elm counterpart. Unguarded `.Value` remains rejected without an approved fallback. Explicit scalar `default(T)` uses the equivalent constant. |
| D6 decimal division | Map to Decimal.div when the C# method result is `decimal?`. | The SDK result is Maybe Decimal. Plain `decimal` division and unlisted precision/scale members remain rejected. |
| D8 enums/reference tuples | Enum → named Int alias and named constants. System.Tuple → standard Elm tuple. | CLR width and reference identity are not represented. |
| D9 constructors/members | Primary parameters → same-named record fields. An explicit constructor that assigns every field → record-building function. Getter/operator bodies → named functions, called at use sites. | No body inlining. Mutation, identity/cycles, side effects, and implicit default initialization remain rejected. |

The user approved the five initial mappings, then approved the nullable Decimal division example.
These decisions replace conflicting earlier rules for argument order, defaults, params, and data functions.

## Existing rejection boundary

The user approved explicit rejection of unmapped forms until a specific Elm mapping is approved.
Keep mutation, identity/cycles, side-effecting construction, and other unmapped forms rejected.
Complex values, integer bit operations without a standard SDK counterpart, larger tuple projections,
and cross-carrier implicit conversions still need a specific mapping.
Do not create compatibility modules or custom output tags for them.

## Implementation evidence

New cases cover owner selection, project options/failures, return branches, written casts,
scalar SDK members/constants, local functions/delegates, tuple reads/destructuring,
and record equality/updates. See the [case registry](../../fixtures/elm-parity/cases.json).
Elm rejects local parameter shadowing. That form remains rejected; names are not changed to make it pass.
All approved mappings are implemented. See [current validation](../deliveries/elm-parity-state.md).
