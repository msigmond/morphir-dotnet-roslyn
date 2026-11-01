# Framework coverage

Keep reference pack 10.0.0 and Roslyn 5.0.0.
Every supported mapping requires [complete Elm compiler parity](../elm-parity.md).

Delivered scalar carriers use direct SDK operations. Decimal literals use fromFloat;
scalar construction uses fromInt/fromFloat. Listed arithmetic, comparisons, conversions,
constants, string conversions, and methods have independent Elm inputs.
Decimal division requires a nullable Decimal result and uses Decimal.div.
Configured metadata calls/getters need an exact target and signature; pure/total flags do not gate them.

The [numeric](numeric-members.json) and [Decimal](decimal-members.json) inventories retain their pinned declarations.
The D1–D10 gate has no pending type/member entries: each is mapped and exact-tested, or explicitly rejected.
Unlisted precision/scale signatures, Complex, unsupported constants/constructors/conversions,
and methods with no approved SDK counterpart remain rejected.
D10 admits generic source declarations; it adds no framework mappings.
Generic metadata calls without an approved target remain rejected.
Later owner entries and full framework closure remain planned.

Input maintenance is separate from validation:

```bash
sh eng/audit-member-inputs.sh --write
python3 eng/generate-member-parity-inputs.py --audit artifacts/d1d9-inventory/results.json --write
node eng/generate-exact-references.mjs --write
```

The audit records source expressions and diagnostics. It does not supply expected IR.
Expected JSON comes only from the independent Elm compiler.
