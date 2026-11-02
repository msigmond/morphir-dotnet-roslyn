# Language coverage

The [exact Elm contract](../elm-parity.md) controls acceptance.
Each supported catalog entry names complete paired compiler tests.
Each rejected D1–D13 entry names executable source rejection tests.

| Delivery | Approved profile |
| --- | --- |
| D1 | Source inputs, records, names, owners, access flags, literals, fields, overload suffixes |
| D2 | Project graphs, generators, additional inputs, Debug/Release options, atomic failures |
| D3 | Locals, scope, returned branches, early returns, written unreachable branches |
| D4 | Nullable types, constructors/defaults, map/map2/withDefault, presence tests, inferred locals |
| D5 | Scalar carriers, arithmetic, comparisons, mapped casts, remainder, listed SDK members |
| D6 | Decimal construction, operations/members, nullable-result division |
| D7 | Normalized calls/defaults/params, references/delegates, captures, local binding graphs, recursion, configured metadata |
| D8 | Enum aliases/constants, value/reference tuples, projections and written deconstruction |
| D9 | Data aliases, pure constructor/member functions, record equality/updates |
| D10 | Unconstrained generic aliases/methods/locals and instantiated calls; tested constraint/dispatch rejections |
| D11 | List/Dict/Set carriers, construction, spreads, mapped reads, and nullable-result indexing |
| D12 | Returned switches, direct literal/enum/nullable/tuple/list patterns, and case bindings |
| D13 | Lambdas, anonymous methods, lexical captures, curried functions, construction, and invocation |
| D11 follow-up | Filter, map, seeded fold adapter, List copies, callbacks, and chained calls |

Unmapped forms retain explicit rejection. See [mappings and limits](../classic-output.md)
and [unsupported features with examples](../unsupported-language-features.md).
The [registry](../../fixtures/elm-parity/cases.json) has 89 C# cases and 71 independent Elm projects.
The member grids cover 320 admitted input forms. Separate nullable division tests cover both spellings.
There are 431 executable member/form rejections.
D14–D21 remain planned. Source v1 and normalized comparisons do not establish acceptance.

The [single-file robot-rental demo](../../samples/FestivalRobotRental/README.md) covers the approved business feature families. Earlier sample cases remain regression inputs.
