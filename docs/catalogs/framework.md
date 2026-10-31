# Framework coverage

The reference pack remains 10.0.0; Roslyn remains 5.0.0.
Acceptance requires [exact Elm parity](../elm-parity.md) for every supported feature.

Primitive type mappings and listed arithmetic/comparison operators have paired compiler references.
Decimal literals use the approved `Decimal.fromFloat` mapping. Decimal add/subtract/multiply/negation use SDK operations.
Complex, unlisted conversions/members, collections, dates/time/Guid, object/boxing, and other unapproved mappings remain pending.
Metadata calls require an exact configured target and signature; a real Elm dependency fixture verifies the complete output.

[framework.json](framework.json) records type-family coverage.
[numeric-members.json](numeric-members.json) and [decimal-members.json](decimal-members.json) retain the full pinned inventories.
Only entries with exact case evidence are supported. Inventory presence alone is not support.
No CLR proof or compatibility algorithm is an acceptance condition.
