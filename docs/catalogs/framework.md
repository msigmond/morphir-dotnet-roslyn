# Framework source coverage

Use the [Classic mappings](../classic-output.md) by default; [source mode](../source-structure.md) retains the broader syntax domain. The reference pack remains 10.0.0 and Roslyn remains 5.0.0.
[framework.json](framework.json) retains every fixed type family.
[numeric-members.json](numeric-members.json) and [decimal-members.json](decimal-members.json) retain the exact pinned public declaration inventories.
Inventory presence does not imply complete dependent-signature support.

| Family | Current mapping |
| --- | --- |
| Bool, String, Char | Simple SDK types and literals; explicit nullable types alone use Maybe |
| Integer families, native64, 128-bit, BigInteger | Simple Int projection; exact source domain in metadata |
| Half, Single, Double | Simple Float projection; exact source precision in metadata |
| Decimal | SDK Decimal, decimal literals, direct add/sub/mul; other explicit source operations |
| Complex, Func, reference tuples | Named intrinsic source types and written members |
| Scalar external APIs | Resolved source calls; no DLL body translation or pure/total assertion |
| Arrays/collections beyond scoped params | D11 source mapping pending |
| Dates, time, durations, Guid | D16 source mapping pending |
| Object/boxing and other remaining type forms | D17 source mapping pending |

Calls retain written arguments and resolved signatures. Members with unsupported signature types retain a delivery diagnostic.
Generic/ref/collection/resource/async syntax retains its separate gate.
Full API closure still requires signature, source-node, sample, and fixture coverage; no runtime algorithm or safety proof is required.
Compatibility libraries and former numeric transport rules are historical and are not emitted.
