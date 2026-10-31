# Language coverage

The [exact Elm parity contract](../elm-parity.md) controls acceptance.
The [case registry](../../fixtures/elm-parity/cases.json) records paired inputs and feature coverage.
Every `supported` catalog entry names its exact-parity case IDs.
Source v1 success and normalized comparisons do not count as acceptance.

| Area | Exact-tested subset |
| --- | --- |
| D1 | Literals, records, names, visibility, documentation, fields, simple operations |
| D2 | Project/source inputs, project references, generated partial source, additional files, configuration |
| D3 | Initialized locals, returns, empty statements, conditional expressions, Boolean operations |
| D4 | Explicit nullable types, reference types without implicit Maybe, null-forgiving context |
| D5 | Integer widths, Single/Double/Half operations, written float literals, comparisons, unary operations |
| D6 | Decimal arithmetic, negation, and approved literal constructor mapping |
| D7 | Complete positional calls, instance receivers, recursion, exact metadata/dependency contracts |
| D8 | Value tuple types and construction |
| D9 | Record/class/struct aliases, stored properties/fields, positional record constructors |

These are subsets. The machine catalogs retain all remaining mapping gaps.
Mutation, identity/cycles, general construction, nullable value operations, and other unmapped forms fail explicitly.
D10–D21 remain planned. See [current evidence](../deliveries/elm-parity-state.md).
